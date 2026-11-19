using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var services = _context.Services.Where(s => s.IsActive).AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                services = services.Where(s => s.ServiceName.Contains(searchString) || s.Category.Contains(searchString));
            }
            ViewData["CurrentFilter"] = searchString;
            return View(await services.ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            // Validate logic to avoid SQL crash
            if (service.BasePrice < 0) ModelState.AddModelError("BasePrice", "Giá cơ bản không được âm.");
            if (service.WeightTierMin < 0) ModelState.AddModelError("WeightTierMin", "Cân nặng tối thiểu không được âm.");
            if (service.WeightTierMax < service.WeightTierMin) ModelState.AddModelError("WeightTierMax", "Cân nặng tối đa phải lớn hơn hoặc bằng cân nặng tối thiểu.");

            if (ModelState.IsValid)
            {
                service.ServiceId = Guid.NewGuid();
                service.IsActive = true;
                _context.Add(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm dịch vụ thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var service = await _context.Services.FindAsync(id);
            if (service == null || !service.IsActive) return NotFound();
            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Service service)
        {
            if (id != service.ServiceId) return NotFound();

            // Validate logic to avoid SQL crash
            if (service.BasePrice < 0) ModelState.AddModelError("BasePrice", "Giá cơ bản không được âm.");
            if (service.WeightTierMin < 0) ModelState.AddModelError("WeightTierMin", "Cân nặng tối thiểu không được âm.");
            if (service.WeightTierMax < service.WeightTierMin) ModelState.AddModelError("WeightTierMax", "Cân nặng tối đa phải lớn hơn hoặc bằng cân nặng tối thiểu.");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingService = await _context.Services.FindAsync(id);
                    if (existingService == null) return NotFound();

                    existingService.ServiceName = service.ServiceName;
                    existingService.Category = service.Category;
                    existingService.Description = service.Description;
                    existingService.DurationMinutes = service.DurationMinutes;
                    existingService.BasePrice = service.BasePrice;
                    existingService.WeightTierMin = service.WeightTierMin;
                    existingService.WeightTierMax = service.WeightTierMax;

                    _context.Update(existingService);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Services.Any(e => e.ServiceId == service.ServiceId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                service.IsActive = false;
                _context.Update(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã ngừng cung cấp dịch vụ!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}