using Microsoft.AspNetCore.Mvc;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    public class AccountController : Controller
    {
        // SỬA: Dùng ApplicationDbContext thay vì AppDbContext
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // THÊM BẢO MẬT
        public IActionResult DangKy(string Username, string Password, string ConfirmPassword,
                                    string FullName, string PhoneNumber, string Email, string Address)
        {
            if (Password != ConfirmPassword)
            {
                ViewBag.Loi = "Mật khẩu nhập lại không khớp!";
                return View();
            }

            if (_context.Accounts.Any(x => x.Username == Username))
            {
                ViewBag.Loi = "Tên đăng nhập này đã tồn tại!";
                return View();
            }

            var newAccount = new Account
            {
                Username = Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                FullName = FullName, // Gán FullName
                Role = "Customer",
                Status = true
            };
            _context.Accounts.Add(newAccount);
            _context.SaveChanges();

            var newCustomer = new Customer
            {
                AccountId = newAccount.Id,
                CustomerName = FullName, // Gán CustomerName từ FullName
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

        [HttpGet]
        public IActionResult DangNhap()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangNhap(string username, string password)
        {
            var user = _context.Accounts.FirstOrDefault(x => x.Username == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                if (!user.Status)
                {
                    ViewBag.Loi = "Tài khoản của bạn đã bị khóa!";
                    return View();
                }

                HttpContext.Session.SetString("AccountId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Role", user.Role);

                // SỬA: Bỏ chuyển hướng Admin, chỉ về Home
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Loi = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return View();
        }

        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("DangNhap");
        }

        // ==========================================
        // BỔ SUNG: CHỨC NĂNG ĐỔI MẬT KHẨU (UC02)
        // ==========================================
        [HttpGet]
        public IActionResult DoiMatKhau()
        {
            if (HttpContext.Session.GetString("AccountId") == null)
                return RedirectToAction("DangNhap");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiMatKhau(string oldPassword, string newPassword, string confirmPassword)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (string.IsNullOrEmpty(accountIdStr)) return RedirectToAction("DangNhap");

            var accountId = Guid.Parse(accountIdStr);
            var user = _context.Accounts.Find(accountId);

            if (user == null) return RedirectToAction("DangNhap");

            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            {
                ViewBag.Loi = "Mật khẩu cũ không chính xác!";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Loi = "Mật khẩu mới nhập lại không khớp!";
                return View();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // BỔ SUNG: QUẢN LÝ TÀI KHOẢN CHO ADMIN (UC03)
        // ==========================================
        [HttpGet]
        public IActionResult QuanLyTaiKhoan()
        {
            // Kiểm tra quyền Admin
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("DangNhap");

            var dsTaiKhoan = _context.Accounts.ToList();
            return View(dsTaiKhoan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KhoaTaiKhoan(Guid id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("DangNhap");

            var user = _context.Accounts.Find(id);
            if (user != null)
            {
                user.Status = !user.Status; // Đảo trạng thái
                _context.SaveChanges();
            }
            return RedirectToAction("QuanLyTaiKhoan");
        }
    }
}