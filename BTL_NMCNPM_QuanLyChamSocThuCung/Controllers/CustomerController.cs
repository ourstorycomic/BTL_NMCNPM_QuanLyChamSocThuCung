using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var customers = _context.Customers.Where(c => c.IsActive).AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(c => c.CustomerName.Contains(searchString) || c.PhoneNumber.Contains(searchString));
            }
            ViewData["CurrentFilter"] = searchString;
            return View(await customers.OrderByDescending(c => c.CreatedAt).ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            // FR-10: Check trùng số điện thoại
            var phoneExists = await _context.Customers.AnyAsync(c => c.PhoneNumber == customer.PhoneNumber && c.IsActive);
            if (phoneExists)
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được đăng ký cho một khách hàng khác.");
            }

            if (ModelState.IsValid)
            {
                customer.Id = Guid.NewGuid();
                customer.CreatedAt = DateTime.Now;
                customer.IsActive = true;
                _context.Add(customer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null || !customer.IsActive) return NotFound();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Customer customer)
        {
            if (id != customer.Id) return NotFound();

            // FR-10: Check trùng SĐT (trừ chính bản ghi đang sửa)
            var phoneExists = await _context.Customers.AnyAsync(c => c.PhoneNumber == customer.PhoneNumber && c.Id != id && c.IsActive);
            if (phoneExists)
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được đăng ký cho một khách hàng khác.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // FIX THEO FEEDBACK: Lấy bản ghi gốc, chỉ cập nhật các trường cho phép
                    var existingCustomer = await _context.Customers.FindAsync(id);
                    if (existingCustomer == null) return NotFound();

                    existingCustomer.CustomerName = customer.CustomerName;
                    existingCustomer.PhoneNumber = customer.PhoneNumber;
                    existingCustomer.Email = customer.Email;
                    existingCustomer.Address = customer.Address;
                    // Giữ nguyên CreatedAt và AccountId

                    _context.Update(existingCustomer);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Customers.Any(e => e.Id == customer.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                customer.IsActive = false;
                _context.Update(customer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã ngừng hoạt động khách hàng!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}