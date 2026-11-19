using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    /// <summary>
    /// Module Quản lý Hóa đơn & Thanh toán (UC12 / FR-20, FR-21, FR-22)
    /// Phụ trách: Nguyễn Hà Phương
    /// </summary>
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BillingController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. DANH SÁCH HÓA ĐƠN & CUỘC HẸN CHỜ THANH TOÁN
        // ==========================================
        public async Task<IActionResult> Index(string? keyword, string? status, string? paymentMethod)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // 1.1. Thống kê KPI nhanh trong ngày
            var todayPaidInvoices = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.PaymentStatus == "Paid" && i.PaymentTime >= today && i.PaymentTime < tomorrow)
                .ToListAsync();

            var todayRevenue = todayPaidInvoices.Sum(i => i.TotalAmount);
            var todayPaidCount = todayPaidInvoices.Count;

            // 1.2. Danh sách các cuộc hẹn chưa thanh toán xong (Processing hoặc Completed nhưng chưa có hóa đơn Paid)
            var pendingApptQuery = _db.Appointments
                .AsNoTracking()
                .Include(a => a.Pet)
                    .ThenInclude(p => p.Customer)
                .Include(a => a.AppointmentServices)
                    .ThenInclude(s => s.Service)
                .Include(a => a.Invoice)
                .Include(a => a.ReceptionTicket)
                .Where(a => (a.Status == "Processing" || a.Status == "Completed") &&
                            (a.Invoice == null || a.Invoice.PaymentStatus != "Paid"))
                .OrderBy(a => a.AppointmentDate);

            // 1.3. Danh sách hóa đơn
            var invoiceQuery = _db.Invoices
                .AsNoTracking()
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.Pet)
                        .ThenInclude(p => p.Customer)
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.AppointmentServices)
                        .ThenInclude(s => s.Service)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var key = keyword.Trim();
                invoiceQuery = invoiceQuery.Where(i =>
                    i.Appointment.Pet.PetName.Contains(key) ||
                    i.Appointment.Pet.Customer.CustomerName.Contains(key) ||
                    i.Appointment.Pet.Customer.PhoneNumber.Contains(key) ||
                    i.InvoiceId.ToString().Contains(key));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                invoiceQuery = invoiceQuery.Where(i => i.PaymentStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(paymentMethod) && paymentMethod != "All")
            {
                invoiceQuery = invoiceQuery.Where(i => i.PaymentMethod == paymentMethod);
            }

            var invoices = await invoiceQuery
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var pendingAppointments = await pendingApptQuery.ToListAsync();

            var model = new BillingIndexViewModel
            {
                Keyword = keyword,
                Status = status ?? "All",
                PaymentMethod = paymentMethod ?? "All",
                TodayRevenue = todayRevenue,
                TodayPaidCount = todayPaidCount,
                PendingCheckoutCount = pendingAppointments.Count,
                PendingAppointments = pendingAppointments,
                Invoices = invoices
            };

            return View(model);
        }

        // ==========================================
        // 2. MÀN HÌNH LẬP HÓA ĐƠN & THANH TOÁN (UC12 / 4.7.1.4)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Checkout(Guid id)
        {
            // id có thể là AppointmentId hoặc InvoiceId
            var appointment = await _db.Appointments
                .Include(a => a.Pet)
                    .ThenInclude(p => p.Customer)
                .Include(a => a.AppointmentServices)
                    .ThenInclude(s => s.Service)
                .Include(a => a.ReceptionTicket)
                .Include(a => a.Invoice)
                    .ThenInclude(i => i!.InvoiceDetails)
                .FirstOrDefaultAsync(a => a.AppointmentId == id || (a.Invoice != null && a.Invoice.InvoiceId == id));

            if (appointment == null)
            {
                TempData["Error"] = "Không tìm thấy lịch hẹn hoặc hóa đơn yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            var pet = appointment.Pet;
            var customer = pet.Customer;
            var ticket = appointment.ReceptionTicket;
            var invoice = appointment.Invoice;

            var services = new List<BillingServiceItemViewModel>();
            if (appointment.AppointmentServices != null && appointment.AppointmentServices.Any())
            {
                foreach (var aps in appointment.AppointmentServices)
                {
                    services.Add(new BillingServiceItemViewModel
                    {
                        ServiceId = aps.ServiceId,
                        ServiceName = aps.Service.ServiceName,
                        Category = aps.Service.Category,
                        Quantity = aps.Quantity > 0 ? aps.Quantity : 1,
                        UnitPrice = aps.Service.BasePrice
                    });
                }
            }

            // Nếu cuộc hẹn chưa có dịch vụ nào gắn kèm thì lấy dịch vụ cơ bản
            if (services.Count == 0)
            {
                var defaultSvc = await _db.Services.FirstOrDefaultAsync(s => s.IsActive);
                if (defaultSvc != null)
                {
                    services.Add(new BillingServiceItemViewModel
                    {
                        ServiceId = defaultSvc.ServiceId,
                        ServiceName = defaultSvc.ServiceName,
                        Category = defaultSvc.Category,
                        Quantity = 1,
                        UnitPrice = defaultSvc.BasePrice
                    });
                }
            }

            var model = new BillingCheckoutViewModel
            {
                AppointmentId = appointment.AppointmentId,
                InvoiceId = invoice?.InvoiceId,
                CustomerName = customer.CustomerName,
                PhoneNumber = customer.PhoneNumber,
                Email = customer.Email,
                Address = customer.Address,
                PetName = pet.PetName,
                Species = pet.Species,
                Breed = pet.Breed,
                ActualWeight = ticket?.ActualWeight ?? pet.Weight,
                AllergyNote = pet.AllergyNote,
                AppointmentDate = appointment.AppointmentDate,
                Services = services,
                Surcharge = invoice?.Surcharge ?? 0,
                SurchargeReason = invoice?.SurchargeReason,
                DiscountCode = invoice?.DiscountCode,
                DiscountAmount = invoice?.DiscountAmount ?? 0,
                PaymentMethod = invoice?.PaymentMethod ?? "Cash"
            };

            return View(model);
        }

        // ==========================================
        // 3. API KIỂM TRA MÃ KHUYẾN MÃI (FR-21 / UC12)
        // ==========================================
        [HttpPost]
        public IActionResult CheckPromo([FromBody] PromoCheckRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
            }

            var code = req.Code.Trim().ToUpperInvariant();
            decimal discount = 0;
            string description = "";

            switch (code)
            {
                case "PAWNEW":
                    discount = Math.Min(req.Subtotal * 0.10m, 50000m); // Giảm 10% tối đa 50k
                    description = "Giảm 10% cho khách hàng mới (tối đa 50.000đ)";
                    break;
                case "PAWHAVEN":
                    discount = 30000m; // Giảm cố định 30k
                    description = "Ưu đãi tri ân thương hiệu PawHaven (-30.000đ)";
                    break;
                case "VIP20":
                    discount = 20000m;
                    description = "Mã thành viên thân thiết (-20.000đ)";
                    break;
                case "SPA50":
                    discount = 50000m;
                    description = "Voucher chăm sóc sắc đẹp cao cấp (-50.000đ)";
                    break;
                default:
                    return Json(new { success = false, message = $"Mã khuyến mãi '{req.Code}' không tồn tại hoặc đã hết hạn." });
            }

            return Json(new
            {
                success = true,
                code = code,
                discountAmount = discount,
                description = description,
                message = $"Áp dụng thành công mã {code}: -{discount:N0}đ"
            });
        }

        // ==========================================
        // 4. XỬ LÝ THANH TOÁN & LẬP HÓA ĐƠN (UC12 / FR-20, FR-22)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(BillingProcessPaymentModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu thanh toán không hợp lệ.";
                return RedirectToAction(nameof(Checkout), new { id = model.AppointmentId });
            }

            var appointment = await _db.Appointments
                .Include(a => a.AppointmentServices)
                    .ThenInclude(s => s.Service)
                .Include(a => a.Invoice)
                    .ThenInclude(i => i!.InvoiceDetails)
                .Include(a => a.Pet)
                    .ThenInclude(p => p.Customer)
                .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);

            if (appointment == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin cuộc hẹn.";
                return RedirectToAction(nameof(Index));
            }

            // 4.1. Tính toán giá gốc dịch vụ
            var subtotal = appointment.AppointmentServices.Sum(x => x.Quantity * x.Service.BasePrice);
            if (subtotal == 0) subtotal = 100000m; // fallback nếu chưa có

            var surcharge = Math.Max(0, model.Surcharge);
            var discount = Math.Max(0, model.DiscountAmount);
            var finalTotal = Math.Max(0, subtotal + surcharge - discount);

            var method = model.PaymentMethod is "QRCode" ? "QRCode" : "Cash";

            // 4.2. Khởi tạo hoặc cập nhật Hóa đơn (Invoice)
            var invoice = appointment.Invoice;
            if (invoice == null)
            {
                invoice = new Invoice
                {
                    AppointmentId = appointment.AppointmentId,
                    Surcharge = surcharge,
                    SurchargeReason = string.IsNullOrWhiteSpace(model.SurchargeReason) ? null : model.SurchargeReason.Trim(),
                    DiscountCode = string.IsNullOrWhiteSpace(model.DiscountCode) ? null : model.DiscountCode.Trim().ToUpperInvariant(),
                    DiscountAmount = discount,
                    TotalAmount = finalTotal,
                    PaymentMethod = method,
                    PaymentStatus = "Paid",
                    PaymentTime = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Invoices.Add(invoice);
            }
            else
            {
                invoice.Surcharge = surcharge;
                if (!string.IsNullOrWhiteSpace(model.SurchargeReason))
                {
                    invoice.SurchargeReason = model.SurchargeReason.Trim();
                }
                invoice.DiscountCode = string.IsNullOrWhiteSpace(model.DiscountCode) ? null : model.DiscountCode.Trim().ToUpperInvariant();
                invoice.DiscountAmount = discount;
                invoice.TotalAmount = finalTotal;
                invoice.PaymentMethod = method;
                invoice.PaymentStatus = "Paid";
                invoice.PaymentTime = DateTime.UtcNow;
            }

            // 4.3. Tạo chi tiết hóa đơn (InvoiceDetail - chuẩn 3NF)
            // Xóa các chi tiết cũ nếu có trước khi thêm mới
            if (invoice.InvoiceDetails != null && invoice.InvoiceDetails.Any())
            {
                _db.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
            }

            foreach (var apptSvc in appointment.AppointmentServices)
            {
                _db.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId = invoice.InvoiceId,
                    ServiceId = apptSvc.ServiceId,
                    Quantity = apptSvc.Quantity > 0 ? apptSvc.Quantity : 1,
                    UnitPrice = apptSvc.Service.BasePrice,
                    Amount = (apptSvc.Quantity > 0 ? apptSvc.Quantity : 1) * apptSvc.Service.BasePrice
                });
            }

            // 4.4. Đóng lịch hẹn: chuyển thành Completed
            appointment.Status = "Completed";

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã thanh toán thành công hóa đơn #{invoice.InvoiceId.ToString().Substring(0, 6).ToUpper()} ({finalTotal:N0}đ)!";
            return RedirectToAction(nameof(Print), new { id = invoice.InvoiceId });
        }

        // ==========================================
        // 5. IN BIÊN LAI / HÓA ĐƠN (FR-22 / UC12)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Print(Guid id)
        {
            var invoice = await _db.Invoices
                .AsNoTracking()
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Service)
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.Pet)
                        .ThenInclude(p => p.Customer)
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.ReceptionTicket)
                        .ThenInclude(t => t!.Staff)
                .FirstOrDefaultAsync(i => i.InvoiceId == id || i.AppointmentId == id);

            if (invoice == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn cần in.";
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        // ==========================================
        // 6. XÁC NHẬN THANH TOÁN NHANH TẠI QUẦY (Quick Pay)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickPay(Guid invoiceId, string method)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Appointment)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
            {
                TempData["Error"] = "Hóa đơn không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            invoice.PaymentMethod = method == "QRCode" ? "QRCode" : "Cash";
            invoice.PaymentStatus = "Paid";
            invoice.PaymentTime = DateTime.UtcNow;
            invoice.Appointment.Status = "Completed";

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã xác nhận thanh toán hóa đơn #{invoice.InvoiceId.ToString().Substring(0, 6).ToUpper()}!";
            return RedirectToAction(nameof(Index));
        }
    }

    public class PromoCheckRequest
    {
        public string Code { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
    }
}
