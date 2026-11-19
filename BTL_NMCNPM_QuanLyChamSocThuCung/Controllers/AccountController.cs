using Microsoft.AspNetCore.Mvc;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult DangKy() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKy(
            string Username,
            string Password,
            string ConfirmPassword,
            string FullName,
            string PhoneNumber,
            string Email,
            string Address)
        {
            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(FullName))
            {
                ViewBag.Loi = "Vui lòng nhập tên đăng nhập, mật khẩu và họ tên.";
                return View();
            }

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

            if (!string.IsNullOrWhiteSpace(PhoneNumber) &&
                _context.Customers.Any(c => c.PhoneNumber == PhoneNumber))
            {
                ViewBag.Loi = "Số điện thoại này đã được đăng ký!";
                return View();
            }

            var account = new Account
            {
                Username = Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                FullName = FullName.Trim(),
                Role = "Customer",
                Status = true
            };

            var customer = new Customer
            {
                Account = account,
                CustomerName = FullName.Trim(),
                PhoneNumber = PhoneNumber?.Trim(),
                Email = Email?.Trim(),
                Address = Address?.Trim(),
                IsActive = true
            };

            _context.Accounts.Add(account);
            _context.Customers.Add(customer);
            _context.SaveChanges();

            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(DangNhap));
        }

        [HttpGet]
        public IActionResult DangNhap() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangNhap(string username, string password)
        {
            var user = _context.Accounts.FirstOrDefault(x => x.Username == username);

            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewBag.Loi = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                return View();
            }

            if (!user.Status)
            {
                ViewBag.Loi = "Tài khoản của bạn đã bị khóa!";
                return View();
            }

            HttpContext.Session.SetString("AccountId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(DangNhap));
        }

        [HttpGet]
        public IActionResult QuenMatKhau() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuenMatKhau(
            string username,
            string phoneNumber,
            string newPassword,
            string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(phoneNumber))
            {
                ViewBag.Loi = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.Loi = "Vui lòng nhập mật khẩu mới.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Loi = "Mật khẩu nhập lại không khớp!";
                return View();
            }

            var account = _context.Accounts
                .FirstOrDefault(a => a.Username == username.Trim());

            var customer = account == null
                ? null
                : _context.Customers.FirstOrDefault(c =>
                    c.AccountId == account.Id &&
                    c.PhoneNumber == phoneNumber.Trim());

            if (account == null || customer == null)
            {
                ViewBag.Loi = "Tên đăng nhập hoặc số điện thoại không chính xác.";
                return View();
            }

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.SaveChanges();

            TempData["Success"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
            return RedirectToAction(nameof(DangNhap));
        }
    }
}