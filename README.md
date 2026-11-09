# LifeOS — Quản lý cá nhân

Bản thay thế mã ứng dụng Second Brain bằng chương trình Quản lý cá nhân HTML/CSS/JS:
bảng tổng hợp, lịch, deadline, thu chi, công nợ, ghi chú và các chủ đề Sáng/Tối đổi màu.

- `frontend/`: đúng giao diện Quản lý cá nhân, build tĩnh cho Vercel, không còn Vue.
- `backend/`: API ASP.NET Core 8 mới cho Render, lưu MySQL/TiDB và nhận Webhook SePay.
- `tests/`: kiểm tra parser, phiên đăng nhập, sổ giao dịch; không dùng dữ liệu thật.

Giữ Root Directory Vercel là `frontend`, build `npm run build`, output `dist`.
Giữ Root Directory Render là `backend` và dùng Dockerfile. Chi tiết: [DEPLOYMENT.md](DEPLOYMENT.md).

Backend tạo bảng riêng có tiền tố `LifeOs`. Không xóa bảng/database Second Brain cũ.
Tài khoản/dữ liệu cũ không tự chuyển: sau cập nhật, đăng ký tài khoản LifeOS riêng.
Không có tài khoản mặc định admin/123.

## Kết nối SePay

Đăng nhập → Cài đặt → Kết nối VietinBank & SePay → Tạo kết nối. Sao chép URL và API Key
vào Webhook SePay; chọn cả tiền vào/ra và bảo mật API Key. Dùng JSON, bật tự gửi lại.
Máy chủ chỉ lưu bản băm của khóa. Không gửi khóa, mật khẩu hoặc OTP qua chat.

Webhook dùng khóa duy nhất `(ConnectionId, SePayId)` chống trùng. Tiền ra ghi chi,
tiền vào ghi thu. Web kiểm tra thay đổi mỗi 15 giây khi mở; máy chủ vẫn nhận khi đóng web.
Đây là ghi sổ, không chuyển/rút tiền ngân hàng. Số dư ứng dụng là tổng thu trừ chi đã ghi.
Rà soát khoản nhập tay trùng, chuyển nội bộ và trả nợ. Điều kiện VietinBank/SEVQR vẫn áp
dụng; không tự nhập giao dịch lịch sử trước khi tạo Webhook.

## Kiểm tra mã

```powershell
npm.cmd --prefix frontend ci
npm.cmd --prefix frontend run build
npm.cmd --prefix frontend test
dotnet build backend/PersonalFinance.API.csproj
dotnet run --project tests/LifeOSChecks
```

Bản trong thư mục Quản lý cá nhân ban đầu tiếp tục dùng offline được. `frontend/` của
repo này dành cho Vercel và cần backend. Test mã không thay thế kiểm tra MySQL staging.
