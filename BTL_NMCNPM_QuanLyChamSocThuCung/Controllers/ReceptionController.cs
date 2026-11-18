using BTL_NMCNPM_QuanLyChamSocThuCung.Data;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers;

public class ReceptionController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public ReceptionController(ApplicationDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    // Danh sách lịch hẹn chờ tiếp nhận và đang chăm sóc.
    public async Task<IActionResult> Index(string? keyword)
    {
        var query = _db.Appointments
            .AsNoTracking()
            .Include(a => a.Pet)
                .ThenInclude(p => p.Customer)
            .Where(a => a.Status == "Confirmed" || a.Status == "Processing")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var key = keyword.Trim();
            query = query.Where(a =>
                a.Pet.PetName.Contains(key) ||
                a.Pet.Customer.CustomerName.Contains(key) ||
                a.Pet.Customer.PhoneNumber.Contains(key));
        }

        var model = new ReceptionIndexViewModel
        {
            Keyword = keyword,
            Appointments = await query.OrderBy(a => a.AppointmentDate).ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var appointment = await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Pet)
                .ThenInclude(p => p.Customer)
            .Include(a => a.AppointmentServices)
                .ThenInclude(x => x.Service)
            .Include(a => a.ReceptionTicket)
                .ThenInclude(t => t!.Staff)
            .Include(a => a.ReceptionTicket)
                .ThenInclude(t => t!.CareProgresses)
                    .ThenInclude(p => p.Staff)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);

        if (appointment == null) return NotFound();

        var model = new ReceptionDetailsViewModel
        {
            Appointment = appointment,
            Ticket = appointment.ReceptionTicket,
            StaffMembers = await _db.Accounts
                .AsNoTracking()
                .Where(a => a.Role == "Staff" && a.Status)
                .OrderBy(a => a.FullName)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(CheckInViewModel model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Details), new { id = model.AppointmentId });

        var appointment = await _db.Appointments
            .Include(a => a.ReceptionTicket)
            .Include(a => a.Pet)
            .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);

        if (appointment == null) return NotFound();

        if (appointment.Status != "Confirmed" || appointment.ReceptionTicket != null)
        {
            TempData["Error"] = "Lịch hẹn không còn ở trạng thái có thể tiếp nhận.";
            return RedirectToAction(nameof(Details), new { id = model.AppointmentId });
        }

        var staffExists = await _db.Accounts.AnyAsync(a =>
            a.Id == model.StaffId && a.Role == "Staff" && a.Status);
        if (!staffExists)
        {
            TempData["Error"] = "Vui lòng chọn nhân viên tiếp nhận hợp lệ.";
            return RedirectToAction(nameof(Details), new { id = model.AppointmentId });
        }

        var ticket = new ReceptionTicket
        {
            AppointmentId = appointment.AppointmentId,
            ActualWeight = model.ActualWeight,
            StaffId = model.StaffId,
            CheckInTime = DateTime.Now
        };

        _db.ReceptionTickets.Add(ticket);
        appointment.Pet.Weight = model.ActualWeight;
        appointment.Status = "Processing";

        if (!string.IsNullOrWhiteSpace(model.InitialCondition))
        {
            _db.CareProgresses.Add(new CareProgress
            {
                TicketId = ticket.TicketId,
                StaffId = model.StaffId,
                StepDescription = "Ghi nhận tình trạng ban đầu",
                ProgressStatus = "Processing",
                StaffNotes = model.InitialCondition.Trim(),
                UpdatedAt = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã tiếp nhận thú cưng thành công.";
        return RedirectToAction(nameof(Details), new { id = model.AppointmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProgress(CareProgressViewModel model)
    {
        if (model.ProgressStatus is not ("Pending" or "Processing" or "Completed"))
            ModelState.AddModelError(nameof(model.ProgressStatus), "Trạng thái chăm sóc không hợp lệ.");

        var ticket = await _db.ReceptionTickets
            .Include(t => t.Appointment)
            .FirstOrDefaultAsync(t => t.TicketId == model.TicketId);

        if (ticket == null) return NotFound();

        var staffExists = await _db.Accounts.AnyAsync(a =>
            a.Id == model.StaffId && a.Role == "Staff" && a.Status);
        if (!staffExists) ModelState.AddModelError(nameof(model.StaffId), "Nhân viên không hợp lệ.");

        if (model.ResultImage != null)
        {
            var extension = Path.GetExtension(model.ResultImage.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                ModelState.AddModelError(nameof(model.ResultImage), "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");

            if (model.ResultImage.Length == 0 || model.ResultImage.Length > 5 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.ResultImage), "Ảnh phải có dung lượng từ 1 byte đến 5 MB.");
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = ticket.AppointmentId });
        }

        string? imageUrl = null;
        if (model.ResultImage != null)
        {
            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "care-results");
            Directory.CreateDirectory(uploadFolder);

            var extension = Path.GetExtension(model.ResultImage.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await model.ResultImage.CopyToAsync(stream);
            imageUrl = $"/uploads/care-results/{fileName}";
        }

        _db.CareProgresses.Add(new CareProgress
        {
            TicketId = model.TicketId,
            StaffId = model.StaffId,
            StepDescription = model.StepDescription.Trim(),
            ProgressStatus = model.ProgressStatus,
            StaffNotes = string.IsNullOrWhiteSpace(model.StaffNotes) ? null : model.StaffNotes.Trim(),
            ResultImageUrl = imageUrl,
            UpdatedAt = DateTime.Now
        });

        // Chỉ chuyển lịch hẹn sang hoàn thành khi bước chăm sóc được đánh dấu hoàn thành.
        if (model.ProgressStatus == "Completed")
            ticket.Appointment.Status = "Completed";
        else
            ticket.Appointment.Status = "Processing";

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật tiến trình chăm sóc.";
        return RedirectToAction(nameof(Details), new { id = ticket.AppointmentId });
    }
}