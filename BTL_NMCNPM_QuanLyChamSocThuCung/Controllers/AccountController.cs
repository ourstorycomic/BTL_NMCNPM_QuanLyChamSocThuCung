using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
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
                PhoneNumber = PhoneNumber?.Trim() ?? string.Empty,
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
        public async Task<IActionResult> DangNhap(string username, string password)
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

            // 1. Lưu thông tin vào Session
            HttpContext.Session.SetString("AccountId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role);

            // Tìm thông tin khách hàng liên kết nếu có
            var customer = _context.Customers.FirstOrDefault(c => c.AccountId == user.Id);
            if (customer != null)
            {
                HttpContext.Session.SetString("CustomerId", customer.Id.ToString());
            }

            // 2. Thiết lập Claims và xác thực Cookie chính thống
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Username", user.Username)
            };

            if (customer != null)
            {
                claims.Add(new Claim("CustomerId", customer.Id.ToString()));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 3. Cookie backup hỗ trợ hiển thị giao diện
            Response.Cookies.Append("UserRole", user.Role, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddHours(8) });
            Response.Cookies.Append("FullName", user.FullName, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddHours(8) });
            if (customer != null)
            {
                Response.Cookies.Append("CustomerId", customer.Id.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddHours(8) });
            }

            TempData["SuccessMessage"] = $"Chào mừng {user.FullName} đã đăng nhập thành công!";

            // 4. Điều hướng theo phân quyền
            if (user.Role == "Staff")
            {
                return RedirectToAction("Index", "Reception");
            }
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Report");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> DangXuat()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("UserRole");
            Response.Cookies.Delete("FullName");
            Response.Cookies.Delete("CustomerId");
            Response.Cookies.Delete("PetCare_ActiveRole");
            TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống an toàn.";
            return RedirectToAction("Index", "Home");
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