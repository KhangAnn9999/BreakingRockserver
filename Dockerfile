# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file
COPY ["GameInventoryApi.csproj", "./"]

# Restore dependencies
RUN dotnet restore "GameInventoryApi.csproj"

# Copy source code
COPY . .

# Build application
RUN dotnet build "GameInventoryApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "GameInventoryApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables for Render
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD dotnet --version || exit 1

# Run application
ENTRYPOINT ["dotnet", "GameInventoryApi.dll"]
