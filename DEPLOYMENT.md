# Cập nhật Personal OS

Thay đổi này sửa xác thực, quyền sở hữu dữ liệu, đồng bộ, giao diện Hôm nay, ghi trả nợ từ ví và thông báo đa thiết bị. Chưa triển khai lên Vercel/Render và chưa kết nối kiểm thử cơ sở dữ liệu production.

## Trước khi cập nhật

1. Sao lưu database. Giữ lại LocalStorage và các ghi chú `__SB_CLOUD_VAULT_V2__` cũ; không xóa dữ liệu trình duyệt khi đang chuyển đổi.
2. Cập nhật backend và frontend trong cùng một đợt bảo trì. Backend mới không còn chấp nhận `X-User-Id`; frontend cũ không dùng được với backend mới.
3. Người dùng cần đăng nhập lại. Cache cũ chỉ được chuyển sang tài khoản khi ID người đăng nhập trùng ID chủ sở hữu đã ghi trong trình duyệt. Cache của mỗi tài khoản được tách riêng.

## Backend trên Render

Thiết lập biến môi trường:

| Biến | Nội dung |
| --- | --- |
| `ConnectionStrings__MySqlConnection` | Chuỗi kết nối MySQL/TiDB đang sử dụng |
| `Cors__Origins__0` | `https://second-brain-app-sepia.vercel.app` |
| `VAPID_PUBLIC_KEY` | Khóa công khai mới |
| `VAPID_PRIVATE_KEY` | Khóa riêng mới, chỉ đặt trên backend |
| `VAPID_SUBJECT` | `mailto:` kèm địa chỉ liên hệ thực tế |
| `DataProtection__KeyPath` | Thư mục trên ổ lưu bền, ví dụ `/var/data/personal-os-keys` |
| `GEMINI_API_KEY` | Tùy chọn, dùng AI ngoài thay cho bộ phân tích cục bộ |
| `GEMINI_MODEL` | Tên model đã được bật trong tài khoản của bạn |

Tạo cặp khóa VAPID mới trên máy quản trị:

```powershell
dotnet run --project backend -- --generate-vapid
```

Không đưa khóa riêng vào frontend hoặc Git. Đã xóa cặp khóa dự phòng viết cứng trong source. Nếu bản cũ đang dùng cặp khóa đó, thay cả hai khóa và tắt/bật đăng ký thông báo trên từng thiết bị.

Phiên đăng nhập được bảo vệ bằng ASP.NET Core Data Protection, có hạn 7 ngày và bị vô hiệu khi mật khẩu thay đổi. Thư mục khóa phải được lưu bền; các instance backend phải dùng chung key ring. Nếu không cấu hình lưu bền, khởi động lại có thể yêu cầu người dùng đăng nhập lại.

## Database

Ứng dụng áp dụng bản nâng cấp cộng thêm `backend/Schema/20261105.sql` và ghi nhận vào `AppSchemaVersions`. Bổ sung:

- `Users.NotificationSettings`.
- `Transactions.TransferId`, `Transactions.DebtRecordId`.
- Bảng `ReminderDeliveries` theo dõi kết quả gửi từng thiết bị.
- Các bảng/cột cũ còn thiếu theo bootstrap của dự án.

Không xóa bảng hoặc dữ liệu. Chỉ bỏ qua lỗi cột/index đã tồn tại; lỗi khác làm startup thất bại để tránh chạy với schema thiếu. Chưa xác minh script với database thực của bạn, vì vậy cần thử trên bản sao/staging trước production.

Đây là cơ chế nâng cấp schema theo phiên bản dành cho database đã bootstrap trước đây. Không chạy lại migration `InitialCreate` cũ trên database production. Nếu đội vận hành áp dụng SQL thủ công, xử lý các cột đã tồn tại, ghi nhận version sau khi hoàn tất và đặt `Database__AutoUpgrade=false`.

Khoản nợ trong Cloud Vault cũ được nhập sang `/api/debts` một lần, giữ ID và ID đã xóa. Ghi chú cũ được giữ làm bản sao lưu và ẩn khỏi màn hình ghi chú. Cờ chuyển số dư giữa các đợt hiện lưu riêng trên từng thiết bị.

## Frontend trên Vercel

- Root Directory: `frontend`.
- Build Command: `npm run build`.
- Output Directory: `dist`.
- `VITE_API_URL`: `https://<backend-domain>/api`.

Đường dẫn thông báo dùng `/?tab=calendar`, `/?tab=tasks`, `/?tab=debts`, không yêu cầu rewrite cho đường dẫn con.

## Kiểm tra sau deploy

1. Đăng nhập lại và xác nhận ví, khoản nợ, công việc, lịch, ghi chú cũ vẫn đủ.
2. Dùng hai tài khoản để kiểm tra không nhìn thấy dữ liệu/cache của nhau.
3. Tạo một giao dịch; làm mới trên thiết bị khác và đối chiếu số dư.
4. Thử mất mạng, tạo công việc, kết nối lại và chọn đồng bộ. Khi lỗi, kiểm tra hàng đợi trong Cài đặt. Thao tác bị từ chối không được đánh dấu thành công; có thể bỏ thao tác đầu nếu dữ liệu không hợp lệ. Bản sao thao tác bỏ vẫn nằm trên thiết bị.
5. Ghi trả nợ từ ví: đối chiếu số dư và số đã trả; đây là ghi sổ trong ứng dụng, không chuyển tiền ngân hàng.
6. Vào **Cài đặt → Bật thông báo → Gửi thử** trên từng thiết bị. iPhone/iPad cần mở web từ Màn hình chính trên hệ điều hành hỗ trợ Web Push.
7. Tạo lịch trong giờ tới, một công việc sắp hạn và khoản nợ trong 3 ngày tới. Kiểm tra giờ yên lặng và nhấp thông báo mở đúng tab.

Worker nhắc lịch cần backend hoạt động liên tục. Nếu hosting tạm dừng tiến trình, nhắc lịch có thể bị bỏ lỡ; các nhắc nợ/công việc được kiểm tra lại khi tiến trình hoạt động. Việc gửi push có thể bị chậm/chặn bởi hệ điều hành hoặc mạng. Nếu crash xảy ra sau khi nhà cung cấp nhận push nhưng trước khi lưu kết quả, lần thử sau có thể gửi trùng.

## Giới hạn còn lại

- Lịch và deadline dùng giờ Việt Nam/UTC+7; giao dịch lưu thời điểm UTC. Dữ liệu lịch cũ từng được ghi UTC cần đối chiếu và chỉnh nếu lệch giờ; không tự dịch hàng loạt vì định dạng cũ lẫn lộn.
- Đồng bộ xếp hàng theo thiết bị và giữ ID khi gửi lại. Hai thiết bị sửa cùng một bản ghi vẫn theo lần lưu sau cùng; chưa có màn hình giải quyết xung đột hoặc cộng tác thời gian thực.
- Giao dịch chuyển ví mới có liên kết hai đầu; giao dịch cũ không có `TransferId` không được tự đoán liên kết.
- Luồng “Ghi trả nợ từ ví” lưu giao dịch tương ứng. Đánh dấu hoàn thành thủ công trên khoản cũ vẫn chỉ thay đổi trạng thái, không tự trừ tiền lần nữa.
- Có xuất sao lưu JSON; chưa có nhập/khôi phục, thu chi định kỳ, mục tiêu tiết kiệm hay báo cáo nâng cao trong đợt sửa này.
- Đã build và chạy kiểm tra mã; chưa kiểm chứng hiển thị desktop/mobile bằng ảnh trình duyệt, chưa thử push thực trên thiết bị hoặc giao dịch với database staging.

## Kiểm tra mã

```powershell
npm.cmd --prefix frontend run build
npm.cmd --prefix frontend test
dotnet run --project tests/SecurityChecks
dotnet build backend/PersonalFinance.API.csproj
```

Các kiểm tra tự động không kết nối hoặc sửa dữ liệu production.
