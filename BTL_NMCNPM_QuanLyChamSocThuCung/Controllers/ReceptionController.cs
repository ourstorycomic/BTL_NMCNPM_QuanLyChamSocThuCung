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

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Ví dụ: hai người bấm tiếp nhận cùng lúc -> vi phạm UNIQUE(AppointmentId)
            TempData["Error"] = "Lịch hẹn vừa được người khác tiếp nhận, vui lòng tải lại.";
            return RedirectToAction(nameof(Details), new { id = model.AppointmentId });
        }

        TempData["Success"] = "Đã tiếp nhận thú cưng thành công.";
        return RedirectToAction(nameof(Details), new { id = model.AppointmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProgress(CareProgressViewModel model)
    {
        var ticket = await _db.ReceptionTickets
            .Include(t => t.Appointment)
            .FirstOrDefaultAsync(t => t.TicketId == model.TicketId);

        if (ticket == null) return NotFound();

        // Chỉ cho cập nhật khi lịch hẹn đang xử lý (chặn POST trực tiếp vào lịch đã hoàn thành)
        if (ticket.Appointment.Status != "Processing")
        {
            TempData["Error"] = "Chỉ cập nhật được khi lịch hẹn đang ở trạng thái Đang xử lý.";
            return RedirectToAction(nameof(Details), new { id = ticket.AppointmentId });
        }

        if (model.ProgressStatus is not ("Pending" or "Processing" or "Completed"))
            ModelState.AddModelError(nameof(model.ProgressStatus), "Trạng thái chăm sóc không hợp lệ.");

        var staffExists = await _db.Accounts.AnyAsync(a =>
            a.Id == model.StaffId && a.Role == "Staff" && a.Status);
        if (!staffExists) ModelState.AddModelError(nameof(model.StaffId), "Nhân viên không hợp lệ.");

        var extension = string.Empty;
        if (model.ResultImage != null)
        {
            extension = Path.GetExtension(model.ResultImage.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                ModelState.AddModelError(nameof(model.ResultImage), "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");
            else if (model.ResultImage.Length == 0 || model.ResultImage.Length > 5 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.ResultImage), "Ảnh phải có dung lượng từ 1 byte đến 5 MB.");
            else if (!await HasValidImageSignatureAsync(model.ResultImage, extension))
                ModelState.AddModelError(nameof(model.ResultImage), "Nội dung file không phải ảnh hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = ticket.AppointmentId });
        }

        string? imageUrl = null;
        string? savedPath = null;
        if (model.ResultImage != null)
        {
            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "care-results");
            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            savedPath = Path.Combine(uploadFolder, fileName);

            await using (var stream = new FileStream(savedPath, FileMode.CreateNew))
            {
                await model.ResultImage.CopyToAsync(stream);
            }
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

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Không để lại file ảnh mồ côi khi ghi DB thất bại
            if (savedPath != null && System.IO.File.Exists(savedPath))
                System.IO.File.Delete(savedPath);

            TempData["Error"] = "Không thể lưu tiến trình, vui lòng thử lại.";
            return RedirectToAction(nameof(Details), new { id = ticket.AppointmentId });
        }

        TempData["Success"] = "Đã cập nhật tiến trình chăm sóc.";
        return RedirectToAction(nameof(Details), new { id = ticket.AppointmentId });
    }

    // Kiểm tra "magic bytes" để chắc đúng là file ảnh, không tin đuôi file.
    private static async Task<bool> HasValidImageSignatureAsync(IFormFile file, string ext)
    {
        var h = new byte[12];
        await using var s = file.OpenReadStream();
        var read = await s.ReadAsync(h.AsMemory(0, h.Length));
        if (read < h.Length) return false;

        return ext switch
        {
            ".jpg" or ".jpeg" => h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF,
            ".png" => h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47,
            ".webp" => h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&
                       h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50,
            _ => false
        };
    }
}
