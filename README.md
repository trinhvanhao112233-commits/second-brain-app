# Ứng Dụng Quản Lý Tài Chính Cá Nhân (MVP)

Dự án Full-stack xây dựng theo kiến trúc hiện đại, sẵn sàng cho cả Web và Ứng dụng Di động (Mobile App qua Capacitor).

---

## 📁 Cấu Trúc Dự Án

```text
f:/CODE WORK/Cá nhân/
├── backend/                  # ASP.NET Core 8 Web API & EF Core
│   ├── Controllers/          # WalletsController.cs
│   ├── Data/                 # AppDbContext.cs (SQL Server & MySQL)
│   ├── DTOs/                 # WalletDtos.cs (Request/Response DTOs)
│   ├── Models/               # Wallet.cs & Transaction.cs
│   ├── PersonalFinance.API.csproj
│   ├── Program.cs            # DI, CORS & DbContext setup
│   └── appsettings.json      # Connection strings
│
└── frontend/                 # Vue 3 (Composition API) + TailwindCSS
    ├── capacitor.config.json # Cấu hình ứng dụng Mobile
    ├── index.html            # Hỗ trợ Safe Area (tai thỏ / home bar)
    ├── package.json
    ├── tailwind.config.js
    ├── vite.config.js
    └── src/
        ├── App.vue
        ├── main.js
        ├── style.css
        └── components/
            └── FinanceDashboard.vue   # UI Thẻ ATM, Nạp/Rút trực tiếp
```

---

## 🚀 Hướng Dẫn Khởi Chạy

### 1. Khởi chạy Backend (.NET 8)

Mở terminal tại thư mục `backend`:
```bash
cd "f:\CODE WORK\Cá nhân\backend"

# 1. Khởi tạo Migration và Cập nhật Database
dotnet ef migrations add InitialCreate
dotnet ef database update

# 2. Khởi chạy API
dotnet run
```
API sẽ lắng nghe tại `http://localhost:5000` (hoặc cổng cấu hình trong `launchSettings.json`).
Bạn có thể truy cập Swagger UI tại: `http://localhost:5000/swagger` để kiểm thử các API.

> **Mẹo Database:** Mặc định `AppDbContext` đang dùng SQL Server (`localhost`). Nếu dùng MySQL, mở file [Program.cs](file:///f:/CODE%20WORK/C%C3%A1%20nh%C3%A2n/backend/Program.cs) và bỏ comment dòng `UseMySql`.

---

### 2. Khởi chạy Frontend (Vue 3 + Vite)

Mở một terminal khác tại thư mục `frontend`:
```bash
cd "f:\CODE WORK\Cá nhân\frontend"

# 1. Cài đặt thư viện
npm install

# 2. Chạy môi trường Dev
npm run dev
```
Trang web sẽ chạy tại: `http://localhost:5173`

---

### 3. Đóng gói Ứng Dụng Mobile (Capacitor)

Nếu bạn muốn build app cho Android hoặc iOS:
```bash
cd "f:\CODE WORK\Cá nhân\frontend"

# 1. Build bản web tĩnh
npm run build

# 2. Thêm nền tảng Android / iOS
npx cap add android
npx cap add ios

# 3. Đồng bộ code web vào Native project
npx cap sync

# 4. Mở trong Android Studio / Xcode
npx cap open android
npx cap open ios
```
*Lưu ý: Khi test trên máy thật/máy ảo điện thoại, đổi `API_BASE_URL` trong [FinanceDashboard.vue](file:///f:/CODE%20WORK/C%C3%A1%20nh%C3%A2n/frontend/src/components/FinanceDashboard.vue) từ `localhost` thành IP máy tính của bạn (ví dụ: `http://192.168.1.15:5000/api/wallets`).*
