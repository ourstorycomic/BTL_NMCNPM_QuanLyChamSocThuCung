# Feature: Reception - Tiếp nhận và cập nhật chăm sóc

## Các chức năng
- Danh sách lịch hẹn có trạng thái Confirmed hoặc Processing, tìm theo tên thú cưng/khách hàng/số điện thoại.
- Tiếp nhận lịch hẹn đã xác nhận: ghi nhận nhân viên, cân nặng thực tế, tình trạng ban đầu; tạo ReceptionTicket và chuyển Appointment sang Processing.
- Cập nhật tiến trình chăm sóc, trạng thái, ghi chú và ảnh kết quả.
- Ảnh được lưu tại wwwroot/uploads/care-results với tên GUID; CSDL chỉ lưu đường dẫn tương đối.
- Seed dữ liệu mẫu cho nhân viên, dịch vụ, khách hàng, thú cưng và lịch hẹn nếu các bảng tương ứng chưa có dữ liệu.

## Cách chạy
1. Pull nhánh dev mới nhất hoặc checkout nhánh feature/reception.
2. Kiểm tra connection string DefaultConnection trong appsettings.json.
3. Chạy migration hiện có của nhóm:
   dotnet ef database update
4. Khởi động ứng dụng. ReceptionSeedData được gọi tự động khi chạy ứng dụng.
5. Truy cập /Reception. Lịch hẹn mẫu ở trạng thái Confirmed sẽ xuất hiện.

## Tài khoản mẫu
- Username: staff.demo
- PasswordHash của tài khoản mẫu chỉ là chuỗi đánh dấu, KHÔNG phải mật khẩu đăng nhập hợp lệ. Dự án hiện chưa có quy trình đăng nhập/tạo tài khoản hoàn chỉnh trong nhánh dev. Hãy thay bằng BCrypt hash hợp lệ khi tích hợp xác thực.

## Lưu ý tích hợp
- Chức năng hiện chọn nhân viên trực tiếp từ danh sách tài khoản Role=Staff, Status=true do nhánh dev chưa có cơ chế lấy người dùng đang đăng nhập.
- Khi một bước chăm sóc được đánh dấu Completed, lịch hẹn chuyển Completed. Nếu quy trình thực tế có nhiều bước, nên bổ sung cờ hoàn tất toàn bộ dịch vụ hoặc quy tắc xác nhận của quản lý trước khi hoàn tất lịch hẹn.
- Upload kiểm tra phần mở rộng và dung lượng; nếu triển khai production cần bổ sung kiểm tra MIME/nội dung file và giới hạn quyền truy cập.
- Seed không ghi đè dữ liệu hiện có. Có thể xóa dữ liệu demo trong SQL Server nếu không muốn giữ lại.
