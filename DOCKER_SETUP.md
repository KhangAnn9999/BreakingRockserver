# Docker Setup Guide cho Render

## Tệp được thêm:
- `Dockerfile` - Cấu hình để build và chạy ứng dụng
- `.dockerignore` - Loại trừ các tệp không cần thiết khi build
- `docker-compose.yml` - Dùng cho local development

## Build Docker Image

```bash
docker build -t game-inventory-api:latest .
```

## Chạy container locally

### Sử dụng docker-compose:
```bash
docker-compose up
```

### Hoặc chạy trực tiếp:
```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e MongoDbSettings__ConnectionString="mongodb+srv://..." \
  game-inventory-api:latest
```

## Deploy lên Render

### Bước 1: Push code lên GitHub
```bash
git add .
git commit -m "Add Docker configuration"
git push
```

### Bước 2: Tạo Web Service trên Render.com
1. Đăng nhập vào [Render.com](https://render.com)
2. Click "New" → "Web Service"
3. Kết nối GitHub repository của bạn
4. Cấu hình:
   - **Name**: game-inventory-api
   - **Region**: Chọn gần nhất
   - **Branch**: main (hoặc branch của bạn)
   - **Runtime**: Docker
   - **Build Command**: (để trống, Docker sẽ tự build)
   - **Start Command**: (để trống)

### Bước 3: Thiết lập Environment Variables
Trong tab "Environment", thêm các biến:
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:10000
MongoDbSettings__ConnectionString=mongodb+srv://...
MongoDbSettings__DatabaseName=GameInventoryDb
JwtSettings__SecretKey=SuperSecretKeyForGNS3012025ChangeInProduction!
JwtSettings__Issuer=GNS301
JwtSettings__Audience=GameClient
JwtSettings__ExpiryInMinutes=1440
```

> **⚠️ QUAN TRỌNG**: Render tự động gán port 10000. Đừng hardcode port trong code.

### Bước 4: Deploy
Click "Deploy" và chờ hoàn tất. Render sẽ:
1. Kéo code từ GitHub
2. Build Docker image
3. Chạy container
4. Cấp URL công khai (vd: `https://game-inventory-api.onrender.com`)

## Xử lý sự cố

### Container không khởi động
```bash
# Kiểm tra logs
docker logs <container-id>
```

### Kết nối MongoDB thất bại
- Đảm bảo MongoDB Atlas whitelist IP của Render
- Kiểm tra connection string là chính xác

### Port issues
Render mặc định dùng port 10000. Dockerfile đã cấu hình để lắng nghe trên port này thông qua `ASPNETCORE_URLS`.

## Local Testing trước khi deploy

```bash
# Build image
docker build -t game-inventory-api:latest .

# Chạy container
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  game-inventory-api:latest

# Test API
curl http://localhost:8080/swagger
```

## Ghi chú

- Dockerfile sử dụng multi-stage build để giảm kích thước image
- `.dockerignore` loại trừ `bin/` và `obj/` để tăng tốc độ build
- `docker-compose.yml` hữu ích cho development với môi trường giống production
- Hãy kiểm tra `appsettings.json` và sử dụng environment variables cho sensitive data
