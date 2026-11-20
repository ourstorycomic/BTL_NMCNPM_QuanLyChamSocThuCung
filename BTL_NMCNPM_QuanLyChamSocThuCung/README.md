# Hệ Thống Quản Lý Dịch Vụ Chăm Sóc Thú Cưng - PawHaven

Dự án Bài tập lớn môn Nhập môn Công nghệ Phần mềm.  
Hệ thống được xây dựng trên nền tảng ASP.NET Core 8 MVC, phục vụ quy trình chăm sóc thú cưng khép kín: từ khâu khách hàng đặt lịch trực tuyến, nhân viên tiếp nhận và cập nhật tiến trình chăm sóc tại quầy, cho đến thanh toán, in hóa đơn và báo cáo doanh thu.

---

## 1. Yêu cầu môi trường

Để cài đặt và chạy thử dự án, máy tính cần đáp ứng các điều kiện sau:

- Hệ điều hành: Windows 10/11, macOS hoặc Linux.
- .NET 8.0 SDK (hoặc phiên bản mới hơn). Kiểm tra bằng lệnh: `dotnet --version`.
- Hệ quản trị cơ sở dữ liệu: SQL Server 2019/2022, SQL Server Express hoặc LocalDB.
- Công cụ phát triển (tùy chọn): Visual Studio 2022 (khuyến nghị có cài đặt gói ASP.NET and web development), Visual Studio Code hoặc JetBrains Rider.

---

## 2. Hướng dẫn cài đặt và khởi chạy nhanh

### Bước 1: Mở mã nguồn dự án

Mở thư mục dự án bằng Visual Studio 2022 (chọn tệp `BTL_NMCNPM_QuanLyChamSocThuCung.sln`) hoặc mở cửa sổ dòng lệnh (Terminal/PowerShell) tại thư mục chứa tệp `.csproj`:

```bash
cd BTL_NMCNPM_QuanLyChamSocThuCung/BTL_NMCNPM_QuanLyChamSocThuCung
```

### Bước 2: Kiểm tra cấu hình chuỗi kết nối cơ sở dữ liệu

Mở tệp `appsettings.json` tại thư mục dự án và kiểm tra chuỗi kết nối `DefaultConnection`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PetCareManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Tùy theo cấu hình SQL Server trên máy tính, có thể điều chỉnh lại giá trị `Server=...` như sau:
- Sử dụng LocalDB mặc định của Visual Studio:  
  `Server=(localdb)\\mssqllocaldb;Database=PetCareManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`
- Sử dụng bản SQL Server Express:  
  `Server=localhost\\SQLEXPRESS` hoặc `Server=.\\SQLEXPRESS`
- Sử dụng xác thực bằng tài khoản sa:  
  `Server=localhost;Database=PetCareManagementDb;User Id=sa;Password=MatKhauCuaBan;TrustServerCertificate=True;`

### Bước 3: Khởi tạo cơ sở dữ liệu và nạp dữ liệu mẫu

Dự án đã được tích hợp cơ chế tự động thực thi Migration (`db.Database.MigrateAsync()`) và nạp dữ liệu mẫu (`ReceptionSeedData.InitializeAsync()`) ngay trong lần đầu khởi chạy ứng dụng ở môi trường Development.

Nếu muốn chủ động tạo cơ sở dữ liệu bằng dòng lệnh trước khi chạy, thực hiện:

```bash
dotnet restore
dotnet ef database update
```

### Bước 4: Khởi chạy ứng dụng

- Chạy bằng dòng lệnh:
  ```bash
  dotnet run
  ```
- Hoặc trong Visual Studio: Nhấn phím F5 (chọn profile https hoặc http).

### Bước 5: Truy cập hệ thống

Mở trình duyệt web và truy cập theo một trong các địa chỉ sau:
- HTTPS: https://localhost:7003
- HTTP: http://localhost:5279

---

## 3. Danh sách tài khoản dùng thử (Dữ liệu mẫu)

Hệ thống đã chuẩn bị sẵn các tài khoản tương ứng với từng vai trò trong quy trình nghiệp vụ. Mọi tài khoản mẫu đều dùng chung mật khẩu mặc định: `123456`.

| Tên đăng nhập | Mật khẩu | Vai trò (Role) | Người sử dụng | Quyền hạn và phạm vi chức năng |
| --- | --- | --- | --- | --- |
| `admin` | `123456` | Admin | Quản trị viên hệ thống | Quản trị toàn hệ thống: Xem báo cáo thống kê doanh thu (`/Report`), quản lý danh mục dịch vụ, xem toàn bộ hồ sơ thú cưng và danh sách khách hàng. |
| `letan` | `123456` | Staff | Hoàng Thu Trang (Lễ tân) | Tiếp nhận thú cưng tại quầy (`/Reception`), lập hóa đơn và xử lý thanh toán (`/Billing`), duyệt và hủy lịch hẹn (`/Appointment`). |
| `groomer.huy` | `123456` | Staff | Trần Đức Huy (Kỹ thuật viên) | Cập nhật tiến trình chăm sóc thể chất, tải ảnh kết quả thực tế trước và sau khi làm dịch vụ. |
| `minhanh` | `123456` | Customer | Nguyễn Minh Anh | Đặt lịch chăm sóc trực tuyến, quản lý hồ sơ thú cưng cá nhân (bé mèo Mochi), theo dõi lịch hẹn cá nhân. |

Ngoài ra, người dùng có thể tạo thêm tài khoản khách hàng mới bất cứ lúc nào qua trang Đăng ký (`/Account/DangKy`).

---

## 4. Kịch bản kiểm thử luồng nghiệp vụ (End-to-End Walkthrough)

Để kiểm tra toàn diện tính năng của hệ thống, người đánh giá có thể thực hiện theo quy trình khép kín gồm 4 bước dưới đây:

### Kịch bản 1: Khách hàng đặt lịch hẹn chăm sóc
1. Truy cập Trang chủ (`/`), chọn nút "Đăng nhập" ở góc trên bên phải thanh điều hướng.
2. Đăng nhập bằng tài khoản: `minhanh` / `123456`.
3. Truy cập mục "Thú cưng" (`/Pet`): Xem hồ sơ bé mèo Mochi (thông tin giống loài, cân nặng, tiền sử tiêm chủng và cảnh báo dị ứng thành phần ngũ cốc/sulfate).
4. Truy cập mục "Đặt lịch" (`/Appointment/Create`):
   - Chọn bé Mochi.
   - Chọn dịch vụ cần thực hiện (ví dụ: Spa thư giãn thảo mộc hoặc Cắt tỉa lông).
   - Chọn ngày hẹn và khung giờ còn trống (hệ thống tự động tính toán tổng thời lượng và chặn các khung giờ đã trùng lịch).
   - Nhấn "Xác nhận đặt lịch".
5. Lịch hẹn được ghi nhận vào hệ thống và hiển thị trong mục "Lịch hẹn của tôi".

### Kịch bản 2: Tiếp nhận thú cưng và cập nhật tiến trình chăm sóc
1. Đăng xuất tài khoản khách hàng hoặc mở cửa sổ trình duyệt ẩn danh, đăng nhập tài khoản nhân viên: `letan` / `123456`.
2. Truy cập mục "Tiếp nhận" (`/Reception`):
   - Hệ thống hiển thị các lịch hẹn trong ngày cần tiếp nhận.
   - Nhấn "Tiếp nhận" tại lịch hẹn của bé Mochi.
   - Nhập cân nặng thực tế tại quầy (ví dụ: 5.6 kg), chọn kỹ thuật viên phụ trách (Trần Đức Huy) và nhập ghi chú ban đầu về thể trạng của bé.
   - Nhấn lưu: Lịch hẹn chuyển sang trạng thái "Đang chăm sóc" (`Processing`).
3. Truy cập vào chi tiết phiếu tiếp nhận:
   - Cập nhật các bước chăm sóc (ví dụ: "Đã tắm sấy thơm tho", "Đang cắt tỉa tạo kiểu").
   - Tải lên ảnh thực tế sau khi chăm sóc để lưu vào hồ sơ.
   - Khi hoàn thành, chuyển trạng thái phiếu sang hoàn tất.

### Kịch bản 3: Lập hóa đơn, áp mã giảm giá và thanh toán tại quầy
1. Truy cập mục "Hóa đơn & Thanh toán" (`/Billing`):
   - Bảng "Lịch hẹn cần thanh toán & xuất hóa đơn" hiển thị cuộc hẹn vừa hoàn tất (kèm giống loài, thời gian và tạm tính dịch vụ).
   - Nhấn nút "Thanh toán ngay" để mở màn hình thanh toán (`/Billing/Checkout/{id}`).
2. Tại màn hình thanh toán:
   - Hệ thống tự động liệt kê danh sách dịch vụ đã dùng, đơn giá và tổng số tiền.
   - Người dùng có thể nhập thêm phụ phí (nếu có, ví dụ: gỡ rối lông nặng: 50.000đ).
   - Kiểm tra tính năng mã giảm giá bằng cách nhập:
     - `PAWVIP10`: Giảm 10% tổng hóa đơn.
     - `GIAM50K`: Giảm trực tiếp 50.000đ.
     - `PAWHAVEN`: Giảm 30.000đ ưu đãi thương hiệu.
   - Chọn phương thức thanh toán: "Tiền mặt" hoặc "Quét mã QR".
   - Nhấn "Xác nhận thanh toán".
3. Hệ thống tạo hóa đơn thành công và chuyển hướng đến trang In biên lai (`/Billing/Print/{id}`). Trang này được tối ưu sẵn định dạng in ấn/lưu PDF với mã số hóa đơn, thông tin khách hàng, chi tiết dịch vụ, mã QR giao dịch và chữ ký nhân viên.

### Kịch bản 4: Quản trị viên xem báo cáo và thống kê doanh thu
1. Đăng nhập bằng tài khoản quản trị: `admin` / `123456`.
2. Truy cập mục "Báo cáo" (`/Report`):
   - Xem các chỉ số KPI: Doanh thu thực thu trong kỳ, số lượt phục vụ thành công, giá trị đơn hàng trung bình.
   - Biểu đồ phân bổ doanh thu theo danh mục dịch vụ (Spa, Vệ sinh, Làm đẹp, Lưu trú, Y tế).
   - Tra cứu danh sách lịch sử giao dịch thanh toán và hỗ trợ xuất báo cáo ra tệp Excel (`.xlsx`).

---

## 5. Cấu trúc thư mục mã nguồn

```
BTL_NMCNPM_QuanLyChamSocThuCung/
|-- BTL_NMCNPM_QuanLyChamSocThuCung.sln    # Solution chính của dự án
|-- README.md                              # Tài liệu hướng dẫn cài đặt và vận hành hệ thống
|-- BTL_NMCNPM_QuanLyChamSocThuCung/        # Thư mục mã nguồn ứng dụng web ASP.NET Core
    |-- Controllers/                       # Xử lý luồng nghiệp vụ của các module
    |   |-- AccountController.cs           # Đăng ký, đăng nhập, xác thực Cookie và phân quyền
    |   |-- HomeController.cs              # Trang chủ, hiển thị dịch vụ động từ cơ sở dữ liệu
    |   |-- PetController.cs               # Quản lý hồ sơ thú cưng, sổ tiêm chủng, cảnh báo dị ứng
    |   |-- AppointmentController.cs       # Đặt lịch trực tuyến, tính thời lượng, chặn trùng giờ
    |   |-- ReceptionController.cs         # Tiếp nhận tại quầy, cập nhật tiến trình chăm sóc
    |   |-- BillingController.cs           # Lập hóa đơn, kiểm tra mã ưu đãi, xuất biên lai
    |   |-- ReportController.cs            # Thống kê doanh thu, báo cáo KPI, xuất dữ liệu Excel
    |-- Models/                            # Các thực thể CSDL (Account, Customer, Pet, Appointment, Invoice...)
    |-- ViewModels/                        # Các lớp đối tượng truyền dữ liệu giữa Controller và Razor View
    |-- Data/                              # ApplicationDbContext, cấu hình Fluent API, Migrations và dữ liệu mẫu (Seed)
    |-- Services/                          # Tầng nghiệp vụ xử lý logic (PetService, IPetService)
    |-- Views/                             # Giao diện người dùng Razor View (.cshtml)
    |-- wwwroot/                           # Tài nguyên tĩnh: CSS, JavaScript, thư viện Bootstrap và ảnh tải lên
    |-- appsettings.json                   # Cấu hình chuỗi kết nối CSDL và thông số hệ thống
    |-- Program.cs                         # Khởi tạo ứng dụng, cấu hình Middleware, DI và Authentication
```

---

## 6. Xử lý một số sự cố thường gặp

### Sự cố 1: Không kết nối được cơ sở dữ liệu (SqlException)
- Dấu hiệu: Ứng dụng báo lỗi "Cannot connect to server" hoặc "A network-related or instance-specific error occurred".
- Cách khắc phục:
  1. Mở cửa sổ `Services` trong Windows (nhấn Windows + R, gõ `services.msc`), tìm dịch vụ `SQL Server (MSSQLSERVER)` hoặc `SQL Server (SQLEXPRESS)` và bấm `Start` nếu đang bị dừng.
  2. Mở tệp `appsettings.json`, kiểm tra tên máy chủ trong phần `Server=...` đã trùng khớp với tên cấu hình SQL Server trên máy bạn chưa.
  3. Tham số `TrustServerCertificate=True` đã được cài sẵn để xử lý lỗi chứng chỉ kết nối trên môi trường cục bộ.

### Sự cố 2: Trùng cổng mạng (Port already in use)
- Dấu hiệu: Ứng dụng báo cổng 7003 hoặc 5279 đang bị một chương trình khác chiếm dụng.
- Cách khắc phục:
  Mở tệp `Properties/launchSettings.json`, tìm đến dòng `applicationUrl` và đổi số cổng sang giá trị khác (ví dụ: `https://localhost:7005;http://localhost:5285`).

### Sự cố 3: Đăng nhập tài khoản mẫu không thành công
- Tất cả tài khoản mẫu (`admin`, `letan`, `groomer.huy`, `minhanh`) đều dùng mật khẩu là `123456`.
- Mật khẩu được mã hóa an toàn bằng thư viện BCrypt. Nếu đã từng sửa đổi dữ liệu trước đó, bạn có thể xóa cơ sở dữ liệu trong SQL Server và khởi chạy lại ứng dụng để hệ thống tự động sinh lại dữ liệu mẫu chuẩn từ đầu.

---

## 7. Công nghệ sử dụng trong dự án

- Ngôn ngữ & Nền tảng: C# 12, .NET 8.0 (ASP.NET Core MVC).
- Cơ sở dữ liệu: Microsoft SQL Server, Entity Framework Core 8 (Code-First với Migrations).
- Xác thực & Phân quyền: Cookie Authentication kết hợp Claims-based Authorization và ASP.NET Core Session.
- Bảo mật mật khẩu: Thư viện BCrypt.Net-Next.
- Giao diện người dùng: HTML5, CSS3, JavaScript (Fetch API), Bootstrap 5.3, Bootstrap Icons.
- Xuất báo cáo: Thư viện ClosedXML (kết xuất bảng kê giao dịch ra tệp Excel định dạng `.xlsx`).
