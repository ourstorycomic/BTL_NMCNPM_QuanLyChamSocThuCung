using System.ComponentModel.DataAnnotations;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels;

public class ReceptionIndexViewModel
{
    public string? Keyword { get; set; }
    public List<Appointment> Appointments { get; set; } = new();
    public List<Account> StaffMembers { get; set; } = new();
    public List<Service> AvailableServices { get; set; } = new();
}

public class ReceptionDetailsViewModel
{
    public Appointment Appointment { get; set; } = null!;
    public ReceptionTicket? Ticket { get; set; }
    public List<Account> StaffMembers { get; set; } = new();
}

public class CheckInViewModel
{
    [Required]
    public Guid AppointmentId { get; set; }

    [Required]
    [Range(0.1, 200, ErrorMessage = "Cân nặng thực tế phải lớn hơn 0.")]
    public decimal ActualWeight { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn kỹ thuật viên phụ trách.")]
    public Guid StaffId { get; set; }

    [StringLength(500)]
    public string? InitialCondition { get; set; }
}

public class CareProgressViewModel
{
    [Required]
    public Guid TicketId { get; set; }

    [Required]
    public Guid StaffId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả bước chăm sóc.")]
    [StringLength(255)]
    public string StepDescription { get; set; } = string.Empty;

    [Required]
    public string ProgressStatus { get; set; } = "Processing";

    [StringLength(500)]
    public string? StaffNotes { get; set; }

    public IFormFile? ResultImage { get; set; }

    // UC10: Phụ phí phát sinh đẩy sang Hóa đơn (Invoice)
    [Range(0, 100000000, ErrorMessage = "Phụ phí không được âm.")]
    public decimal Surcharge { get; set; } = 0;

    [StringLength(255)]
    public string? SurchargeReason { get; set; }

    // Tách biệt trạng thái từng bước với trạng thái tổng của lịch hẹn
    public bool CompleteAppointment { get; set; }
}

// UC09: Tiếp nhận khách vãng lai (Walk-in) tại quầy
public class WalkInViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên khách hàng.")]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại khách hàng.")]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên thú cưng.")]
    [StringLength(100)]
    public string PetName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn loài thú cưng.")]
    public string Species { get; set; } = "Chó";

    [StringLength(100)]
    public string? Breed { get; set; }

    public string Gender { get; set; } = "Đực";

    [Required]
    [Range(0.1, 200, ErrorMessage = "Cân nặng phải lớn hơn 0.")]
    public decimal ActualWeight { get; set; } = 1.0m;

    [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 dịch vụ.")]
    public List<Guid> SelectedServiceIds { get; set; } = new();

    [Required(ErrorMessage = "Vui lòng chọn nhân viên tiếp nhận.")]
    public Guid StaffId { get; set; }

    [StringLength(500)]
    public string? InitialCondition { get; set; }
}