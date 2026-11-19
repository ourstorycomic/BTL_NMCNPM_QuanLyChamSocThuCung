using Microsoft.AspNetCore.Mvc;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data; // Đã đổi sang gọi thư mục Data của nhóm
using Microsoft.EntityFrameworkCore; // Cần cho các hàm Async

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    public class AccountController : Controller
    {
        // SỬA LỖI 2: Dùng ApplicationDbContext thay vì AppDbContext
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
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
        [ValidateAntiForgeryToken] // BỔ SUNG BẢO MẬT
        public async Task<IActionResult> DangKy(string Username, string Password, string ConfirmPassword,
                                    string CustomerName, string PhoneNumber, string Email, string Address)
        {
            if (Password != ConfirmPassword)
            {
                ViewBag.Loi = "Mật khẩu nhập lại không khớp!";
                return View();
            }

            if (await _context.Accounts.AnyAsync(x => x.Username == Username))
            {
                ViewBag.Loi = "Tên đăng nhập này đã tồn tại!";
                return View();
            }

            // BỔ SUNG: Gán FullName = CustomerName
            var newAccount = new Account
            {
                Username = Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                FullName = CustomerName,
                Role = "Customer",
                Status = true
            };
            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            var newCustomer = new Customer
            {
                AccountId = newAccount.Id,
                CustomerName = CustomerName,
                PhoneNumber = PhoneNumber,
                Email = Email,
                Address = Address,
                IsActive = true
            };
            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

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
        [ValidateAntiForgeryToken] // BỔ SUNG BẢO MẬT
        public async Task<IActionResult> DangNhap(string username, string password)
        {
            var user = await _context.Accounts.FirstOrDefaultAsync(x => x.Username == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                if (!user.Status)
                {
                    ViewBag.Loi = "Tài khoản của bạn đã bị khóa!";
                    return View();
                }

                HttpContext.Session.SetString("AccountId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName ?? "");
                HttpContext.Session.SetString("Role", user.Role);

                // SỬA LỖI 3: Bỏ chuyển hướng sang Admin, chỉ về Home
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Loi = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return View();
        }

        // ==========================================
        // 3. CHỨC NĂNG ĐĂNG XUẤT
        // ==========================================
        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("DangNhap");
        }

        // ==========================================
        // 4. BỔ SUNG: CHỨC NĂNG ĐỔI MẬT KHẨU (UC02)
        // ==========================================
        [HttpGet]
        public IActionResult DoiMatKhau()
        {
            // Kiểm tra đăng nhập
            if (HttpContext.Session.GetString("AccountId") == null)
            {
                return RedirectToAction("DangNhap");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiMatKhau(string MatKhauCu, string MatKhauMoi, string XacNhanMatKhauMoi)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null) return RedirectToAction("DangNhap");

            if (MatKhauMoi != XacNhanMatKhauMoi)
            {
                ViewBag.Loi = "Mật khẩu mới xác nhận không khớp!";
                return View();
            }

            var accountId = Guid.Parse(accountIdStr);
            var user = await _context.Accounts.FindAsync(accountId);

            if (user == null || !BCrypt.Net.BCrypt.Verify(MatKhauCu, user.PasswordHash))
            {
                ViewBag.Loi = "Mật khẩu cũ không chính xác!";
                return View();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(MatKhauMoi);
            _context.Accounts.Update(user);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Đổi mật khẩu thành công!";
            return View();
        }

        // ==========================================
        // 5. BỔ SUNG: QUẢN LÝ TÀI KHOẢN (UC03 - DÀNH CHO ADMIN)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> QuanLyTaiKhoan()
        {
            // Phân quyền: Chỉ Admin mới được vào
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var danhSach = await _context.Accounts.ToListAsync();
            return View(danhSach);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KhoaTaiKhoan(Guid id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return RedirectToAction("Index", "Home");

            var user = await _context.Accounts.FindAsync(id);
            if (user != null && user.Username != "admin") // Không cho khóa chính admin
            {
                user.Status = !user.Status; // Đảo trạng thái (Khóa/Mở khóa)
                _context.Accounts.Update(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("QuanLyTaiKhoan");
        }
    }
}