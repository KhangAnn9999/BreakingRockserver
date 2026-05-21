using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver; 
using Microsoft.Extensions.Options;
using GameInventoryApi.Models;
using GameInventoryApi.Repositories;
using GameInventoryApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Settings từ file appsettings.json
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Kết nối Singleton Database MongoDB
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var client = new MongoClient(settings.ConnectionString);
    return client.GetDatabase(settings.DatabaseName);
});

// Đăng ký Repo tường minh cho từng bảng (Sửa lỗi Generic hệ thống)
builder.Services.AddScoped<IMongoRepository<User>>(sp => 
    new MongoRepository<User>(sp.GetRequiredService<IMongoDatabase>(), "Users"));

builder.Services.AddScoped<IMongoRepository<InventoryItem>>(sp => 
    new MongoRepository<InventoryItem>(sp.GetRequiredService<IMongoDatabase>(), "InventoryItems"));

builder.Services.AddScoped<IMongoRepository<PlayerProfile>>(sp => 
    new MongoRepository<PlayerProfile>(sp.GetRequiredService<IMongoDatabase>(), "PlayerProfiles"));

// Đăng ký tầng Service
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPlayerProfileService, PlayerProfileService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Cấu hình xác thực JWT Token
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Cấu hình giao diện Swagger có tích hợp nút nhập Token bảo mật (Authorize)
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Vui lòng nhập Token theo cú pháp: Bearer <mã_token_của_bạn>",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
app.MapControllers();

// Tự động sinh dữ liệu ảo (Seed Data) mỗi lần bật Server chạy thử
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    await SeedData.InitializeAsync(database);
}

app.Run();