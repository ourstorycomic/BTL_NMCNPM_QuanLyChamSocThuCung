# Module Lịch hẹn — Phạm Đình An

## Phạm vi

- Danh sách lịch hẹn, lọc theo từ khóa (tên thú cưng / khách hàng / số điện thoại), ngày và trạng thái.
- Đặt lịch: chọn khách hàng → thú cưng → dịch vụ → khung giờ trống.
- Hiển thị cảnh báo dị ứng của thú cưng ngay dưới ô chọn thú cưng.
- Chặn trùng giờ ở cả API hiển thị khung giờ và khi submit form.
- Xác nhận / hủy lịch hẹn.
- **Không thay đổi model, migration hay CSDL.** Module dùng các cột đã có trên nhánh `dev`
  (`Appointment.EndTime`, `Appointment.Status`, `AppointmentService.Quantity`, ...).

## Cấu trúc file

| File | Nội dung |
| --- | --- |
| `Controllers/AppointmentController.cs` | Danh sách, đặt lịch, API khung giờ, chi tiết, xác nhận, hủy |
| `ViewModels/AppointmentViewModels.cs` | ViewModel cho danh sách, form đặt lịch và slot |
| `Views/Appointment/Index.cshtml` | Danh sách + bộ lọc |
| `Views/Appointment/Create.cshtml` | Form đặt lịch + JavaScript chọn slot |
| `Views/Appointment/Details.cshtml` | Chi tiết lịch hẹn, nút xác nhận/hủy |
| `wwwroot/css/appointment.css` | Giao diện module |
| `README-APPOINTMENT.md` | Tài liệu này |

## Thuật toán chặn trùng giờ

1. Thời lượng lịch hẹn = tổng `DurationMinutes` của các dịch vụ đang hoạt động được chọn
   (mặc định 30 phút nếu không có dữ liệu).
2. Mỗi lịch còn hiệu lực (`Pending`, `Confirmed`, `Processing`) được quy về khoảng
   `[AppointmentDate, EndTime)`. Nếu `EndTime` null (dữ liệu cũ) thì cộng thời lượng dịch vụ.
3. Hai khoảng trùng nhau khi `startA < endB && endA > startB` (so sánh nửa mở, nên lịch
   09:00–10:00 không chặn lịch 10:00–10:30).
4. `GET /Appointment/Slots` trả các mốc 30 phút từ 07:00, kèm `available` và `reason`
   (`past` = đã qua, `busy` = trùng lịch).
5. Khi POST, server kiểm tra lại độc lập với JavaScript:
   khách hàng tồn tại, thú cưng thuộc khách hàng, dịch vụ tồn tại và đang hoạt động,
   giờ nằm trong 07:00–20:00, không ở quá khứ, không trùng lịch.
6. Slot cuối phải kết thúc trước 20:00 (ví dụ dịch vụ 60 phút có slot cuối 19:00).

## Đường dẫn

| Method | Route | Chức năng |
| --- | --- | --- |
| GET | `/Appointment` | Danh sách + lọc |
| GET | `/Appointment/Create` | Form đặt lịch |
| POST | `/Appointment/Create` | Lưu lịch hẹn (có kiểm tra trùng giờ) |
| GET | `/Appointment/Slots?date=yyyy-MM-dd&serviceIds=id1,id2` | API khung giờ trống |
| GET | `/Appointment/Details/{id}` | Chi tiết |
| POST | `/Appointment/Confirm/{id}` | Xác nhận lịch |
| POST | `/Appointment/Cancel/{id}` | Hủy lịch |

## Cách chạy

- Cần CSDL SQL Server theo `appsettings.json` và chạy migration của nhánh `dev`.
- Cần có sẵn dữ liệu Khách hàng / Thú cưng / Dịch vụ (từ seed của nhóm hoặc module tiếp nhận).
- Chạy project, mở menu **Lịch hẹn** (sidebar nhân viên) hoặc **Đặt lịch / Lịch hẹn của tôi**
  (header khách hàng), hoặc truy cập `/Appointment`.
- Menu đã được nối vào layout PawHaven mới của nhóm; không còn mục menu riêng kiểu cũ.

## Kiểm thử

Đã chạy harness tạm (không commit vào repo): **59/59 kiểm tra đạt**, gồm:

- chạy trên EF InMemory và chạy lại trên SQL Server LocalDB thật (`PetCareManagementDb`);

- danh sách, bộ lọc, form đặt lịch;
- slot 30/60 phút, định dạng 24h, mốc 07:00–19:30;
- chặn trùng giờ, ngoài giờ làm việc, sai thú cưng, thiếu dịch vụ;
- đặt lịch thành công, cập nhật slot sau khi đặt;
- xác nhận, hủy, hủy lần hai bị từ chối, slot mở lại sau khi hủy;
- lọc theo số điện thoại + trạng thái + ngày.

## Lưu ý tích hợp

- Trạng thái `Processing` / `Completed` do module Tiếp nhận cập nhật; hóa đơn do module Hóa đơn xử lý.
- Kiểm tra trùng giờ nằm ở tầng ứng dụng. Với phạm vi bài tập chạy một instance là đủ;
  nếu triển khai nhiều instance cần bổ sung ràng buộc hoặc khóa ở tầng CSDL.
- Không sửa `Program.cs`, `appsettings.json`, model, migration hay code của thành viên khác.
