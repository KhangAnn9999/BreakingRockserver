# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY GameInventoryApi.csproj ./
RUN dotnet restore "GameInventoryApi.csproj"

# Copy the rest of the source code and build
COPY . ./
RUN dotnet publish "GameInventoryApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Listen on port 80 in the container
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "GameInventoryApi.dll"]
