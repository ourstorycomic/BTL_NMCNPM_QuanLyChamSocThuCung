using Microsoft.AspNetCore.Mvc;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        // Inject DbContext vào Controller
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. CHỨC NĂNG ĐĂNG KÝ
        // ==========================================
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DangKy(string Username, string Password, string ConfirmPassword,
                                    string CustomerName, string PhoneNumber, string Email, string Address)
        {
            // Kiểm tra mật khẩu nhập lại có khớp không
            if (Password != ConfirmPassword)
            {
                ViewBag.Loi = "Mật khẩu nhập lại không khớp!";
                return View();
            }

            // Kiểm tra tên đăng nhập đã tồn tại chưa
            if (_context.Accounts.Any(x => x.Username == Username))
            {
                ViewBag.Loi = "Tên đăng nhập này đã tồn tại! Vui lòng chọn tên khác.";
                return View();
            }

            // 1.1. Tạo tài khoản (Băm mật khẩu bằng BCrypt)
            var newAccount = new Account
            {
                Username = Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                Role = "Customer",
                Status = true
            };
            _context.Accounts.Add(newAccount);
            _context.SaveChanges(); // Lưu để lấy Account.Id sinh ra tự động

            // 1.2. Tạo thông tin khách hàng liên kết với tài khoản vừa tạo (Quan hệ 1-1)
            var newCustomer = new Customer
            {
                AccountId = newAccount.Id,
                CustomerName = CustomerName,
                PhoneNumber = PhoneNumber,
                Email = Email,
                Address = Address
            };
            _context.Customers.Add(newCustomer);
            _context.SaveChanges();

            // Chuyển sang trang đăng nhập kèm thông báo
            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("DangNhap");
        }

        // ==========================================
        // 2. CHỨC NĂNG ĐĂNG NHẬP
        // ==========================================
        [HttpGet]
        public IActionResult DangNhap()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DangNhap(string username, string password)
        {
            var user = _context.Accounts.FirstOrDefault(x => x.Username == username);

            // Kiểm tra tài khoản và so sánh mật khẩu bằng BCrypt
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                if (!user.Status)
                {
                    ViewBag.Loi = "Tài khoản của bạn đã bị khóa!";
                    return View();
                }

                // Lưu thông tin vào Session
                HttpContext.Session.SetString("AccountId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);

                // Phân quyền chuyển hướng (Nếu Admin thì vào trang quản trị, Customer về trang chủ)
                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Loi = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return View();
        }

        // ==========================================
        // 3. CHỨC NĂNG ĐĂNG XUẤT
        // ==========================================
        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear(); // Xóa toàn bộ session
            return RedirectToAction("DangNhap");
        }
    }
}