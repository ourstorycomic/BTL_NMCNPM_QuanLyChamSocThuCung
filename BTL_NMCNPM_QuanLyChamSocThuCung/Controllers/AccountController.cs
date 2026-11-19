using Microsoft.AspNetCore.Mvc;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    public class AccountController : Controller
    {
        // Dùng ApplicationDbContext của nhóm
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: Hiển thị form Đăng ký
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        // 2. POST: Xử lý Đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKy(string Username, string Password, string ConfirmPassword,
                                    string FullName, string PhoneNumber, string Email, string Address)
        {
            // Kiểm tra mật khẩu nhập lại
            if (Password != ConfirmPassword)
            {
                ViewBag.Loi = "Mật khẩu nhập lại không khớp!";
                return View();
            }

            // Kiểm tra Username đã tồn tại chưa
            if (_context.Accounts.Any(x => x.Username == Username))
            {
                ViewBag.Loi = "Tên đăng nhập này đã tồn tại!";
                return View();
            }

            // 1. Tạo tài khoản (Gán FullName để tránh lỗi NULL)
            var newAccount = new Account
            {
                Username = Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                FullName = FullName, // Đã gán FullName
                Role = "Customer",
                Status = true
            };
            _context.Accounts.Add(newAccount);
            _context.SaveChanges(); // Lưu để lấy Account.Id

            // 2. Tạo thông tin khách hàng (Quan hệ 1-1)
            var newCustomer = new Customer
            {
                AccountId = newAccount.Id,
                CustomerName = FullName,
                PhoneNumber = PhoneNumber,
                Email = Email,
                Address = Address,
                IsActive = true
            };
            _context.Customers.Add(newCustomer);
            _context.SaveChanges();

            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("DangNhap");
        }

        // 3. GET: Hiển thị form Đăng nhập
        [HttpGet]
        public IActionResult DangNhap()
        {
            return View();
        }

        // 4. POST: Xử lý Đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangNhap(string username, string password)
        {
            var user = _context.Accounts.FirstOrDefault(x => x.Username == username);

            // Kiểm tra mật khẩu bằng BCrypt
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                if (!user.Status)
                {
                    ViewBag.Loi = "Tài khoản của bạn đã bị khóa!";
                    return View();
                }

                // Lưu Session
                HttpContext.Session.SetString("AccountId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Role", user.Role);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Loi = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return View();
        }

        // 5. Đăng xuất
        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("DangNhap");
        }

        // Các hàm DoiMatKhau, QuanLyTaiKhoan... (giữ nguyên như cũ nếu bạn đã viết)
    }
}