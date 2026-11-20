using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    /// <summary>
    /// Module quản lý lịch hẹn: xem danh sách, đặt lịch (chọn dịch vụ + chọn giờ),
    /// xác nhận và hủy lịch. Phần kiểm tra trùng giờ nằm ở HasConflictAsync/Slots.
    /// </summary>
    public class AppointmentController : Controller
    {
        // Khung giờ làm việc của cơ sở chăm sóc.
        private static readonly TimeSpan OpeningTime = new(7, 0, 0);
        private static readonly TimeSpan ClosingTime = new(20, 0, 0);
        private const int SlotStepMinutes = 30;
        private const int DefaultDurationMinutes = 30;

        // Các trạng thái còn chiếm khung giờ (bị chặn khi đặt lịch mới).
        private static readonly string[] BlockingStatuses = { "Pending", "Confirmed", "Processing" };
        private static readonly string[] AllStatuses = { "Pending", "Confirmed", "Processing", "Completed", "Cancelled" };

        private readonly ApplicationDbContext _db;

        public AppointmentController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. DANH SÁCH LỊCH HẸN
        // ==========================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index(string? keyword, string? status, DateTime? date)
        {
            var query = _db.Appointments
                .AsNoTracking()
                .Include(a => a.Pet)
                    .ThenInclude(p => p.Customer)
                .Include(a => a.AppointmentServices)
                    .ThenInclude(x => x.Service)
                .AsQueryable();

            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                       ?? HttpContext.Session.GetString("UserRole")
                       ?? Request.Cookies["UserRole"];

            // Nếu vai trò là Khách hàng, chỉ hiển thị lịch hẹn thuộc về chính mình
            if (role != "Staff" && role != "Admin")
            {
                var accountIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                                   ?? HttpContext.Session.GetString("AccountId");
                var customerIdStr = User.Claims.FirstOrDefault(c => c.Type == "CustomerId")?.Value
                                    ?? HttpContext.Session.GetString("CustomerId");

                Guid.TryParse(accountIdStr, out var accId);
                Guid.TryParse(customerIdStr, out var custId);

                query = query.Where(a =>
                    (custId != Guid.Empty && a.Pet.CustomerId == custId) ||
                    (accId != Guid.Empty && a.Pet.Customer.AccountId == accId));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var key = keyword.Trim();
                query = query.Where(a =>
                    a.Pet.PetName.Contains(key) ||
                    a.Pet.Customer.CustomerName.Contains(key) ||
                    a.Pet.Customer.PhoneNumber.Contains(key));
            }

            if (!string.IsNullOrWhiteSpace(status) && AllStatuses.Contains(status!))
            {
                query = query.Where(a => a.Status == status);
            }

            if (date.HasValue)
            {
                var from = date.Value.Date;
                var to = from.AddDays(1);
                query = query.Where(a => a.AppointmentDate >= from && a.AppointmentDate < to);
            }

            var model = new AppointmentIndexViewModel
            {
                Keyword = keyword?.Trim(),
                Status = status,
                Date = date,
                Appointments = await query
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync()
            };

            return View(model);
        }

        // ==========================================
        // 2. FORM ĐẶT LỊCH
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create(Guid? serviceId)
        {
            var now = DateTime.Now;
            // Nếu ngoài giờ hoạt động (sau 18:00), mặc định đặt cho ngày mai lúc 09:00
            var defaultDate = now.Hour >= 18 ? DateTime.Today.AddDays(1) : DateTime.Today;
            var defaultTime = now.Hour >= 18 ? "09:00" : (now.Hour < 8 ? "09:00" : $"{now.AddHours(1).Hour:D2}:00");

            var model = new AppointmentCreateViewModel
            {
                AppointmentDate = defaultDate,
                StartTime = defaultTime
            };

            if (serviceId.HasValue)
            {
                model.ServiceIds = new List<Guid> { serviceId.Value };
            }

            await PopulateOptionsAsync(model);

            // Tự động chọn thú cưng đầu tiên nếu khách đã có thú cưng
            if (model.CustomerId.HasValue && model.PetsByCustomer.TryGetValue(model.CustomerId.Value.ToString(), out var pets) && pets.Any())
            {
                model.PetId = pets.First().PetId;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentCreateViewModel model)
        {
            // Nạp lại dữ liệu cho các dropdown/checkbox nếu phải trả về form.
            await PopulateOptionsAsync(model);

            // ---- 2.1. Kiểm tra dịch vụ được chọn ----
            var requestedIds = model.ServiceIds?.Distinct().ToList() ?? new List<Guid>();

            var services = requestedIds.Count == 0
                ? new List<Service>()
                : await _db.Services
                    .AsNoTracking()
                    .Where(s => requestedIds.Contains(s.ServiceId) && s.IsActive)
                    .OrderBy(s => s.Category)
                    .ThenBy(s => s.ServiceName)
                    .ToListAsync();

            if (requestedIds.Count == 0)
            {
                ModelState.AddModelError(nameof(model.ServiceIds), "Vui lòng chọn ít nhất một dịch vụ.");
            }
            else if (services.Count != requestedIds.Count)
            {
                ModelState.AddModelError(nameof(model.ServiceIds), "Có dịch vụ không tồn tại hoặc đã ngừng cung cấp.");
            }

            var duration = services.Sum(s => s.DurationMinutes);
            if (duration <= 0) duration = DefaultDurationMinutes;

            model.TotalDurationMinutes = duration;
            model.TotalPrice = services.Sum(s => s.BasePrice);

            // ---- 2.2. Kiểm tra khách hàng và thú cưng ----
            Customer? customer = null;
            Pet? pet = null;

            if (model.CustomerId.HasValue)
            {
                customer = await _db.Customers
                    .FirstOrDefaultAsync(c => c.Id == model.CustomerId.Value && c.IsActive);

                if (customer == null)
                {
                    ModelState.AddModelError(nameof(model.CustomerId), "Khách hàng không tồn tại hoặc đã ngừng hoạt động.");
                }
                else if (model.PetId.HasValue)
                {
                    pet = await _db.Pets
                        .FirstOrDefaultAsync(p => p.PetId == model.PetId.Value && p.CustomerId == customer.Id);

                    if (pet == null)
                    {
                        ModelState.AddModelError(nameof(model.PetId), "Thú cưng không thuộc khách hàng đã chọn.");
                    }
                }
                else
                {
                    ModelState.AddModelError(nameof(model.PetId), "Vui lòng chọn thú cưng.");
                }
            }
            else
            {
                // Khách vãng lai đặt lịch nhanh (Guest Booking)
                if (string.IsNullOrWhiteSpace(model.GuestCustomerName))
                {
                    ModelState.AddModelError(nameof(model.GuestCustomerName), "Vui lòng nhập họ và tên của bạn.");
                }

                if (string.IsNullOrWhiteSpace(model.GuestPhoneNumber))
                {
                    ModelState.AddModelError(nameof(model.GuestPhoneNumber), "Vui lòng nhập số điện thoại để chúng tôi liên hệ xác nhận.");
                }

                if (string.IsNullOrWhiteSpace(model.GuestPetName))
                {
                    ModelState.AddModelError(nameof(model.GuestPetName), "Vui lòng nhập tên bé cưng.");
                }

                if (ModelState.IsValid)
                {
                    var phone = model.GuestPhoneNumber!.Trim();
                    customer = await _db.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == phone);
                    if (customer == null)
                    {
                        customer = new Customer
                        {
                            CustomerName = model.GuestCustomerName!.Trim(),
                            PhoneNumber = phone,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Customers.Add(customer);
                        await _db.SaveChangesAsync();
                    }

                    var petName = model.GuestPetName!.Trim();
                    pet = await _db.Pets.FirstOrDefaultAsync(p => p.CustomerId == customer.Id && p.PetName.ToLower() == petName.ToLower());
                    if (pet == null)
                    {
                        pet = new Pet
                        {
                            CustomerId = customer.Id,
                            PetName = petName,
                            Species = string.IsNullOrWhiteSpace(model.GuestSpecies) ? "Chó" : model.GuestSpecies.Trim(),
                            Breed = model.GuestBreed?.Trim(),
                            Weight = model.GuestWeight ?? 3.5m,
                            AllergyNote = model.GuestAllergyNote?.Trim()
                        };
                        _db.Pets.Add(pet);
                        await _db.SaveChangesAsync();
                    }
                }
            }

            // ---- 2.3. Kiểm tra thời gian ----
            DateTime? start = null;
            if (model.AppointmentDate.HasValue &&
                !string.IsNullOrWhiteSpace(model.StartTime) &&
                TimeSpan.TryParseExact(model.StartTime, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
            {
                start = model.AppointmentDate.Value.Date.Add(time);
            }

            var end = DateTime.MinValue;
            if (start.HasValue)
            {
                end = start.Value.AddMinutes(duration);

                var opening = start.Value.Date.Add(OpeningTime);
                var closing = start.Value.Date.Add(ClosingTime);

                // Dịch vụ lưu trú (Khách sạn thú cưng 24h) không bị ràng buộc phải trả bé trước 20:00 cùng ngày
                var isBoarding = services.Any(s => s.Category == "Lưu trú" || s.DurationMinutes >= 720);

                if (start.Value < opening || start.Value > closing || (!isBoarding && end > closing))
                {
                    ModelState.AddModelError(nameof(model.StartTime),
                        "Giờ hẹn nhận bé phải nằm trong khung 07:00 - 20:00 và dịch vụ làm đẹp trong ngày cần kết thúc trước 20:00.");
                }
                else if (start.Value < DateTime.Now)
                {
                    ModelState.AddModelError(nameof(model.StartTime), 
                        $"Khung giờ {start.Value:dd/MM/yyyy HH:mm} đã ở trong quá khứ. Vui lòng chọn ngày hoặc giờ tiếp theo.");
                }
                else if (!isBoarding && await HasConflictAsync(start.Value, end))
                {
                    ModelState.AddModelError(nameof(model.StartTime),
                        $"Khung giờ {start.Value:HH:mm} - {end:HH:mm} đã có lịch hẹn khác. Vui lòng chọn giờ khác.");
                }
            }
            else
            {
                ModelState.AddModelError(nameof(model.StartTime), "Vui lòng chọn ngày và giờ hẹn hợp lệ.");
            }

            if (!ModelState.IsValid || start == null || customer == null || pet == null)
            {
                if (customer == null) ModelState.AddModelError(nameof(model.CustomerId), "Vui lòng chọn hoặc nhập thông tin khách hàng.");
                if (pet == null) ModelState.AddModelError(nameof(model.PetId), "Vui lòng chọn thú cưng.");
                if (start == null) ModelState.AddModelError(nameof(model.StartTime), "Thời gian đặt lịch không hợp lệ.");

                var errorList = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(msg => !string.IsNullOrWhiteSpace(msg))
                    .Distinct()
                    .ToList();

                TempData["Error"] = errorList.Count > 0
                    ? string.Join(" ", errorList)
                    : "Thông tin đặt lịch chưa hợp lệ. Vui lòng kiểm tra lại.";

                await PopulateOptionsAsync(model);
                return View(model);
            }

            // ---- 2.4. Lưu lịch hẹn ----
            var appointment = new Appointment
            {
                PetId = pet.PetId,
                AppointmentDate = start.Value,
                EndTime = end,
                Status = "Pending",
                Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim()
            };

            _db.Appointments.Add(appointment);

            foreach (var service in services)
            {
                _db.AppointmentServices.Add(new AppointmentService
                {
                    AppointmentId = appointment.AppointmentId,
                    ServiceId = service.ServiceId,
                    Quantity = 1
                });
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể lưu lịch hẹn. Vui lòng kiểm tra dữ liệu và thử lại.");
                return View(model);
            }

            TempData["Success"] = $"Đã đặt lịch hẹn cho {pet.PetName} lúc {start.Value:HH:mm} ngày {start.Value:dd/MM/yyyy}.";
            return RedirectToAction(nameof(Details), new { id = appointment.AppointmentId });
        }

        // ==========================================
        // 3. API TRẢ VỀ KHUNG GIỜ TRỐNG (dùng cho JavaScript)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Slots(DateTime? date, string? serviceIds)
        {
            if (!date.HasValue)
            {
                return BadRequest(new { message = "Vui lòng chọn ngày hẹn." });
            }

            var ids = ParseServiceIds(serviceIds);

            var services = ids.Count == 0
                ? new List<Service>()
                : await _db.Services
                    .AsNoTracking()
                    .Where(s => ids.Contains(s.ServiceId) && s.IsActive)
                    .ToListAsync();

            var duration = services.Sum(s => s.DurationMinutes);
            if (duration <= 0) duration = DefaultDurationMinutes;

            var day = date.Value.Date;
            var busy = await GetBusyIntervalsAsync(day);
            var now = DateTime.Now;

            var slots = new List<AppointmentSlotViewModel>();

            // Duyệt từng mốc 30 phút từ giờ mở cửa đến khi vừa đủ thời lượng dịch vụ.
            for (var slotStart = day.Add(OpeningTime);
                 slotStart.AddMinutes(duration) <= day.Add(ClosingTime);
                 slotStart = slotStart.AddMinutes(SlotStepMinutes))
            {
                var slotEnd = slotStart.AddMinutes(duration);
                var isPast = slotStart < now;
                var isBusy = busy.Any(b => slotStart < b.End && slotEnd > b.Start);

                slots.Add(new AppointmentSlotViewModel
                {
                    Value = slotStart.ToString(@"HH\:mm"),
                    Label = $"{slotStart:HH\\:mm} - {slotEnd:HH\\:mm}",
                    Available = !isPast && !isBusy,
                    Reason = isPast ? "past" : isBusy ? "busy" : null
                });
            }

            return Json(new
            {
                date = day.ToString("yyyy-MM-dd"),
                durationMinutes = duration,
                totalPrice = services.Sum(s => s.BasePrice),
                slots
            });
        }

        // ==========================================
        // 4. CHI TIẾT LỊCH HẸN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var appointment = await _db.Appointments
                .AsNoTracking()
                .Include(a => a.Pet)
                    .ThenInclude(p => p.Customer)
                .Include(a => a.AppointmentServices)
                    .ThenInclude(x => x.Service)
                .Include(a => a.ReceptionTicket)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // ==========================================
        // 5. XÁC NHẬN / HỦY LỊCH
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> Confirm(Guid id)
        {
            var appointment = await _db.Appointments
                .Include(a => a.Pet)
                    .ThenInclude(p => p.Customer)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (appointment == null) return NotFound();

            if (appointment.Status != "Pending")
            {
                TempData["Error"] = "Chỉ xác nhận được lịch hẹn đang ở trạng thái Chờ xác nhận.";
                return RedirectToAction(nameof(Details), new { id });
            }

            appointment.Status = "Confirmed";
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã xác nhận lịch hẹn của khách hàng {appointment.Pet?.Customer?.CustomerName ?? ""} (bé {appointment.Pet?.PetName ?? ""}) thành công! Lịch hẹn đã sẵn sàng để tiếp nhận tại quầy.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var appointment = await _db.Appointments
                .Include(a => a.Pet)
                    .ThenInclude(p => p.Customer)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (appointment == null) return NotFound();

            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                       ?? HttpContext.Session.GetString("UserRole")
                       ?? Request.Cookies["UserRole"];

            if (role != "Staff" && role != "Admin")
            {
                var accountIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                                   ?? HttpContext.Session.GetString("AccountId");
                var customerIdStr = User.Claims.FirstOrDefault(c => c.Type == "CustomerId")?.Value
                                    ?? HttpContext.Session.GetString("CustomerId");

                Guid.TryParse(accountIdStr, out var accId);
                Guid.TryParse(customerIdStr, out var custId);

                bool isOwner = (custId != Guid.Empty && appointment.Pet.CustomerId == custId) ||
                               (accId != Guid.Empty && appointment.Pet.Customer.AccountId == accId);

                if (!isOwner)
                {
                    TempData["Error"] = "Bạn không có quyền thao tác trên lịch hẹn của khách hàng khác.";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (appointment.Status is not ("Pending" or "Confirmed"))
            {
                TempData["Error"] = "Chỉ hủy được lịch hẹn chưa tiếp nhận.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var hasTicket = await _db.ReceptionTickets.AnyAsync(t => t.AppointmentId == id);
            if (hasTicket)
            {
                TempData["Error"] = "Lịch hẹn đã được tiếp nhận nên không thể hủy.";
                return RedirectToAction(nameof(Details), new { id });
            }

            appointment.Status = "Cancelled";
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã hủy lịch hẹn của bé {appointment.Pet?.PetName ?? ""} thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ==========================================
        // HÀM HỖ TRỢ
        // ==========================================

        /// <summary>Nạp danh sách khách hàng, thú cưng theo khách hàng và dịch vụ đang hoạt động.</summary>
        private async Task PopulateOptionsAsync(AppointmentCreateViewModel model)
        {
            model.Services = await _db.Services
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.ServiceName)
                .ToListAsync();

            var isAuth = User.Identity?.IsAuthenticated == true;
            model.IsAuthenticated = isAuth;

            var role = User.FindFirstValue(ClaimTypes.Role) ?? HttpContext.Session.GetString("UserRole");
            var isStaffOrAdmin = role == "Staff" || role == "Admin";
            model.IsStaffOrAdmin = isStaffOrAdmin;

            if (isStaffOrAdmin)
            {
                // Nhân viên / Admin: Được xem toàn bộ khách hàng để tạo lịch hộ
                model.Customers = await _db.Customers
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CustomerName)
                    .ToListAsync();

                var pets = await _db.Pets
                    .AsNoTracking()
                    .Where(p => p.Customer.IsActive)
                    .OrderBy(p => p.PetName)
                    .ToListAsync();

                model.PetsByCustomer = pets
                    .GroupBy(p => p.CustomerId.ToString())
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(p => new PetOptionViewModel
                        {
                            PetId = p.PetId,
                            PetName = p.PetName,
                            Species = p.Species,
                            Breed = p.Breed,
                            Weight = p.Weight,
                            AllergyNote = p.AllergyNote
                        }).ToList());
            }
            else if (isAuth)
            {
                // Khách hàng đã đăng nhập: CHỈ NẠP DUY NHẤT HỒ SƠ VÀ THÚ CƯNG CỦA CHÍNH HỌ
                var accountIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? HttpContext.Session.GetString("AccountId");
                Customer? currentCust = null;

                if (Guid.TryParse(accountIdStr, out var accountId))
                {
                    currentCust = await _db.Customers
                        .Include(c => c.Pets)
                        .FirstOrDefaultAsync(c => c.AccountId == accountId && c.IsActive);
                }

                if (currentCust == null)
                {
                    var username = User.FindFirstValue("Username") ?? HttpContext.Session.GetString("Username");
                    currentCust = await _db.Customers
                        .Include(c => c.Pets)
                        .FirstOrDefaultAsync(c => (c.Account != null && c.Account.Username == username) || c.Email == username);
                }

                if (currentCust != null)
                {
                    model.CustomerId = currentCust.Id;
                    model.LoggedInCustomerName = currentCust.CustomerName;
                    model.LoggedInPhoneNumber = currentCust.PhoneNumber;
                    model.Customers = new List<Customer> { currentCust };

                    var petList = currentCust.Pets.Select(p => new PetOptionViewModel
                    {
                        PetId = p.PetId,
                        PetName = p.PetName,
                        Species = p.Species,
                        Breed = p.Breed,
                        Weight = p.Weight,
                        AllergyNote = p.AllergyNote
                    }).ToList();

                    model.PetsByCustomer = new Dictionary<string, List<PetOptionViewModel>>
                    {
                        { currentCust.Id.ToString(), petList }
                    };

                    if (petList.Any() && !model.PetId.HasValue)
                    {
                        model.PetId = petList.First().PetId;
                    }
                }
                else
                {
                    model.Customers = new List<Customer>();
                    model.PetsByCustomer = new Dictionary<string, List<PetOptionViewModel>>();
                }
            }
            else
            {
                // Khách vãng lai (CHƯA ĐĂNG NHẬP):
                // Tuyệt đối KHÔNG nạp bất kỳ danh sách khách hàng hay thú cưng nào khác để bảo mật dữ liệu!
                model.Customers = new List<Customer>();
                model.PetsByCustomer = new Dictionary<string, List<PetOptionViewModel>>();
            }
        }

        /// <summary>
        /// Trả về các khoảng thời gian đã bị chiếm trong ngày.
        /// EndTime có thể null với dữ liệu cũ nên phải tính lại từ thời lượng dịch vụ.
        /// </summary>
        private async Task<List<BusyInterval>> GetBusyIntervalsAsync(DateTime day)
        {
            var from = day.Date.AddDays(-1);
            var to = day.Date.AddDays(2);

            var appointments = await _db.Appointments
                .AsNoTracking()
                .Include(a => a.AppointmentServices)
                    .ThenInclude(x => x.Service)
                .Where(a => a.AppointmentDate >= from &&
                            a.AppointmentDate < to &&
                            BlockingStatuses.Contains(a.Status))
                .ToListAsync();

            return appointments
                .Select(a =>
                {
                    var minutes = a.AppointmentServices.Sum(x => x.Service.DurationMinutes);
                    if (minutes <= 0) minutes = DefaultDurationMinutes;

                    return new BusyInterval
                    {
                        AppointmentId = a.AppointmentId,
                        Start = a.AppointmentDate,
                        End = a.EndTime ?? a.AppointmentDate.AddMinutes(minutes)
                    };
                })
                .ToList();
        }

        /// <summary>Kiểm tra khoảng [start, end) có chồng lấn với lịch hẹn còn hiệu lực hay không.</summary>
        private async Task<bool> HasConflictAsync(DateTime start, DateTime end)
        {
            var busy = await GetBusyIntervalsAsync(start.Date);
            return busy.Any(b => start < b.End && end > b.Start);
        }

        private static List<Guid> ParseServiceIds(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<Guid>();

            return raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
        }

        private sealed class BusyInterval
        {
            public Guid AppointmentId { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }
    }
}
