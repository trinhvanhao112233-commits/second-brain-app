# Bản thay thế LifeOS — Vercel + Render

## Phạm vi

Thay toàn bộ mã Second Brain bằng Quản lý cá nhân. Giữ lịch sử Git để khôi phục; không
đẩy .env/token/khóa hoặc cấu hình Sites. API dùng bảng mới `LifeOsUsers`, `LifeOsStates`,
`LifeOsBankConnections`, `LifeOsBankEvents`. Không xóa/sửa các bảng Second Brain.
Tài khoản/dữ liệu Second Brain không tự chuyển sang bản mới: sao lưu bản cũ và đăng ký
tài khoản LifeOS. Không có tài khoản mẫu công khai.

## Vercel

| Mục | Giá trị |
| --- | --- |
| Root Directory | `frontend` |
| Framework Preset | `Other` nếu đang khóa preset Vue/Vite |
| Build Command | `npm run build` |
| Output Directory | `dist` |
| Install Command | `npm ci` |
| `VITE_API_URL` | `https://<backend-render>/api` |

Build tạo config.js từ biến API và sao chép HTML/CSS/JS sang dist; không có thư viện
frontend. Đổi URL backend thì build lại frontend. URL dự phòng trùng backend hiện tại.
`frontend/vercel.json` ghi đè preset thành Other và khai báo build/output/install để
không tiếp tục dùng thiết lập Vue/Vite cũ.

## Render

Root Directory `backend`, Dockerfile `Dockerfile`, cổng 5000 và tên project .NET giữ
tương thích với cấu hình cũ, nhưng mã API đã được viết lại. Giữ các biến đang dùng:

| Biến | Giá trị |
| --- | --- |
| `ConnectionStrings__MySqlConnection` | Chuỗi MySQL/TiDB hiện tại |
| `Cors__Origins__0` | Domain Vercel, không có dấu `/` cuối |
| `DataProtection__KeyPath` | Tùy chọn: thư mục key ring trên ổ bền |

Không cần biến Gemini, VAPID hoặc khóa SePay global. Không tự xóa secret cũ nếu dịch vụ
khác còn dùng. Backend cần quyền CREATE TABLE cho bốn bảng mới và đọc/ghi trên chúng.
Thử schema `Store.Schema` trên staging trước production. Chỉ CREATE IF NOT EXISTS, không
DROP. Nếu key ring không bền, restart có thể yêu cầu đăng nhập lại; Webhook key hash
vẫn giữ trong MySQL và tiếp tục nhận giao dịch.

Deploy Render trước; `GET /api/health` phải trả `ok:true`, rồi deploy Vercel. Nếu đang
bật tự deploy main, push sẽ kích hoạt pipeline theo cấu hình của bạn.

## SePay

1. Đăng ký/đăng nhập LifeOS; mặc định tài khoản trống, không có thu chi mẫu.
2. Cài đặt → Kết nối VietinBank & SePay → nhập số tài khoản đã nhận cả tiền vào/ra
   trên SePay → Tạo kết nối. Một kết nối VietinBank cho mỗi tài khoản LifeOS.
3. SePay → Tích hợp Webhooks → Thêm webhook. Dán URL và API Key từ LifeOS.
4. Chọn cả tiền vào/ra, đúng tài khoản. Bảo mật **API Key**, không chọn HMAC. Header
   gửi đến là `Authorization: Apikey <key>`. Dùng HTTPS và `application/json`.
5. Bật tự gửi lại và kiểm tra bộ lọc. VietinBank có điều kiện nội dung SEVQR; ứng dụng
   không thể nhận giao dịch SePay không gửi.
6. Gửi thử ID 0 không tạo thu chi. Đối chiếu một giao dịch thật trên SePay với LifeOS.
   Không gửi dữ liệu tài chính giả vào production. Replay thật không được ghi hai lần.
7. Nếu quên API Key, Tạo khóa mới rồi cập nhật trên SePay. Khóa cũ ngừng hoạt động.
   Khóa chỉ trả về lần tạo/đổi, không lưu LocalStorage hoặc trả từ endpoint đọc.

Chỉ báo thành công sau khi lưu ledger. Primary key MySQL chặn cùng ID, kể cả callback
đồng thời. Replay giống trả 200; cùng ID khác nội dung trả 409. Số tiền/chiều lấy từ
ledger bất biến; người dùng chỉ đổi nhãn/danh mục/ghi chú. bankSeenIds ghi nhớ khoản
đã nhập/xóa: giữ các ID này trong bản sao lưu để replay không phục hồi khoản đã xóa.

Máy chủ nhận khi đóng web; tab đang mở tải mỗi 15 giây và hiện thông báo trong web.
Chưa có Web Push khi đóng trình duyệt. Render Free ngủ sau 15 phút không có request,
cần khoảng một phút khởi động; Webhook có thể chậm và cần retry. Không cam kết tức thì.

Nguồn: https://developer.sepay.vn/vi/sepay-webhooks/tich-hop-webhook,
https://developer.sepay.vn/vi/sepay-webhooks/xac-thuc,
https://docs.sepay.vn/ket-noi-vietinbank.html, https://render.com/docs/free.

## Kiểm tra

Build .NET/frontend và chạy các test trong README. Test mã không kết nối database thật.
Thử schema/quyền MySQL trên staging, kiểm tra hai tài khoản không đọc dữ liệu của nhau,
đổi mật khẩu/đăng xuất, nhiều tab và xung đột revision. Khi xung đột, tải bản sao lưu
thay đổi trước khi tải lại; không tự ghi đè server. Kiểm tra Webhook thật và số dư ghi sổ.

Nếu rollback, redeploy commit cũ. Bảng cũ còn nguyên, nhưng dữ liệu LifeOS không tự nhập
vào mô hình Second Brain cũ nên hai phiên bản có dữ liệu riêng.
