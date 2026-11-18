using System.ComponentModel.DataAnnotations;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels;

public class ReceptionIndexViewModel
{
    public string? Keyword { get; set; }
    public List<Appointment> Appointments { get; set; } = new();
}

public class CheckInViewModel
{
    [Required]
    public Guid AppointmentId { get; set; }

    [Required]
    public Guid StaffId { get; set; }

    [Range(0.1, 300)]
    public decimal ActualWeight { get; set; }

    [StringLength(500)]
    public string? InitialCondition { get; set; }
}

public class CareProgressViewModel
{
    [Required]
    public Guid TicketId { get; set; }

    [Required]
    public Guid StaffId { get; set; }

    [Required, StringLength(255)]
    public string StepDescription { get; set; } = string.Empty;

    [Required]
    public string ProgressStatus { get; set; } = "Processing";

    public string? StaffNotes { get; set; }

    public IFormFile? ResultImage { get; set; }
}

public class ReceptionDetailsViewModel
{
    public Appointment Appointment { get; set; } = null!;
    public List<Account> StaffMembers { get; set; } = new();
    public ReceptionTicket? Ticket { get; set; }
}