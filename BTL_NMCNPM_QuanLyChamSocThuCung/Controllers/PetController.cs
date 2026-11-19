using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models.ViewModels;
using BTL_NMCNPM_QuanLyChamSocThuCung.Services;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    [Authorize]
    public class PetController : Controller
    {
        private readonly IPetService _petService;
        private const string RoleCookieKey = "PetCare_ActiveRole";

        public PetController(IPetService petService)
        {
            _petService = petService;
        }

        private async Task<(string Role, Guid CustomerId)> GetCurrentContextAsync()
        {
            var (demoCustomerId, _) = await _petService.EnsureDemoDataAsync();

            string? sessionRole = null;
            string? sessionCustomerId = null;
            try
            {
                sessionRole = HttpContext.Session.GetString("UserRole");
                sessionCustomerId = HttpContext.Session.GetString("CustomerId");
            }
            catch
            {
                // Session not available
            }

            // Tự động nhận diện vai trò từ phiên đăng nhập thực tế của hệ thống
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value
                       ?? sessionRole
                       ?? Request.Cookies["UserRole"]
                       ?? Request.Cookies[RoleCookieKey]
                       ?? "Customer";

            if (role != "Staff" && role != "Admin")
            {
                role = "Customer";
            }

            var customerIdStr = User.Claims.FirstOrDefault(c => c.Type == "CustomerId")?.Value
                                ?? sessionCustomerId
                                ?? Request.Cookies["CustomerId"];

            var customerId = Guid.TryParse(customerIdStr, out var parsedGuid) ? parsedGuid : demoCustomerId;

            return (role, customerId);
        }

        // ==========================================
        // 1. Danh sách thú cưng
        // ==========================================
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var (role, customerId) = await GetCurrentContextAsync();
            bool isStaff = role == "Staff";

            ViewBag.ActiveRole = role;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsStaff = isStaff;

            var pets = await _petService.GetPetsAsync(isStaff ? null : customerId, searchTerm, isStaff);
            return View(pets);
        }

        // ==========================================
        // 2. Chi tiết hồ sơ thú cưng (FR-09)
        // ==========================================
        public async Task<IActionResult> Details(Guid id)
        {
            var (role, customerId) = await GetCurrentContextAsync();
            bool isStaff = role == "Staff";

            ViewBag.ActiveRole = role;
            ViewBag.IsStaff = isStaff;

            var pet = await _petService.GetPetDetailAsync(id, customerId, isStaff);
            if (pet == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thú cưng hoặc bạn không có quyền truy cập hồ sơ này!";
                return RedirectToAction(nameof(Index));
            }

            return View(pet);
        }

        // ==========================================
        // 3. Thêm mới thú cưng
        // ==========================================
        public async Task<IActionResult> Create()
        {
            var (role, customerId) = await GetCurrentContextAsync();
            bool isStaff = role == "Staff";

            ViewBag.ActiveRole = role;
            ViewBag.IsStaff = isStaff;

            if (isStaff)
            {
                ViewBag.CustomerOptions = await _petService.GetCustomerOptionsAsync();
            }

            var model = new PetCreateViewModel
            {
                CustomerId = isStaff ? null : customerId,
                Gender = "Đực",
                Weight = 1.0m
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PetCreateViewModel model)
        {
            var (role, customerId) = await GetCurrentContextAsync();
            bool isStaff = role == "Staff";

            ViewBag.ActiveRole = role;
            ViewBag.IsStaff = isStaff;

            if (!ModelState.IsValid)
            {
                if (isStaff)
                {
                    ViewBag.CustomerOptions = await _petService.GetCustomerOptionsAsync();
                }
                return View(model);
            }

            var targetCustomerId = isStaff ? (model.CustomerId ?? customerId) : customerId;
            var (success, errorMessage, petId) = await _petService.CreatePetAsync(model, targetCustomerId);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Lỗi khi tạo thú cưng");
                if (isStaff)
                {
                    ViewBag.CustomerOptions = await _petService.GetCustomerOptionsAsync();
                }
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đã thêm thành công thú cưng {model.PetName} vào hệ thống!";
            return RedirectToAction(nameof(Details), new { id = petId });
        }

        // ==========================================
        // 4. Chỉnh sửa hồ sơ thú cưng
        // ==========================================
        public async Task<IActionResult> Edit(Guid id)
        {
            var (role, customerId) = await GetCurrentContextAsync();
            bool isStaff = role == "Staff";

            ViewBag.ActiveRole = role;
            ViewBag.IsStaff = isStaff;

            var model = await _petService.GetPetForEditAsync(id, customerId, isStaff);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thú cưng hoặc bạn không có quyền chỉnh sửa!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PetEditViewModel model)
        {
            var (role, customerId) = await GetCurrentContextAsync();
            bool isStaff = role == "Staff";

            ViewBag.ActiveRole = role;
            ViewBag.IsStaff = isStaff;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, errorMessage) = await _petService.UpdatePetAsync(model, customerId, isStaff);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Lỗi khi cập nhật");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đã cập nhật hồ sơ thú cưng {model.PetName} thành công!";
            return RedirectToAction(nameof(Details), new { id = model.PetId });
        }

        // ==========================================
        // 5. Xóa thú cưng (Ràng buộc toàn vẹn)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (role, customerId) = await GetCurrentContextAsync();
            bool isStaff = role == "Staff";

            var (success, errorMessage) = await _petService.DeletePetAsync(id, customerId, isStaff);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Đã xóa hồ sơ thú cưng thành công khỏi hệ thống!";
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 6. Quản lý tiêm chủng (Bảng Vaccination - 1NF)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVaccination(VaccinationCreateViewModel model)
        {
            var (role, customerId) = await GetCurrentContextAsync();
            bool isStaff = role == "Staff";

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu tiêm chủng không hợp lệ. Vui lòng kiểm tra lại!";
                return RedirectToAction(nameof(Details), new { id = model.PetId });
            }

            var (success, errorMessage) = await _petService.AddVaccinationAsync(model, customerId, isStaff);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã ghi nhận mũi tiêm '{model.VaccineName}' thành công!";
            }

            return RedirectToAction(nameof(Details), new { id = model.PetId });
        }

        // ==========================================
        // 7. API Dùng chung cho UC07 & UC09 (Tiếp nhận & Đặt lịch)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetMedicalAlert(Guid petId)
        {
            var alert = await _petService.GetPetMedicalAlertAsync(petId);
            if (alert == null) return NotFound(new { message = "Không tìm thấy thú cưng" });
            return Ok(alert);
        }

        // ==========================================
        // 8. Chuyển đổi vai trò kiểm thử (Khách hàng <-> Nhân viên)
        // ==========================================
        public IActionResult SwitchRole(string role, string? returnUrl)
        {
            if (role != "Staff" && role != "Customer") role = "Customer";

            Response.Cookies.Append(RoleCookieKey, role, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

            TempData["SuccessMessage"] = $"Đã chuyển góc nhìn làm việc sang: {(role == "Staff" ? "Nhân viên / Bác sĩ" : "Khách hàng (Chủ nuôi)")}";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
