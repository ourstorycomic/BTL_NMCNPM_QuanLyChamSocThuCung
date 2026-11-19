using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels
{
    /// <summary>
    /// Dữ liệu cho màn hình danh sách lịch hẹn.
    /// </summary>
    public class AppointmentIndexViewModel
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public DateTime? Date { get; set; }
        public List<Appointment> Appointments { get; set; } = new();
    }

    /// <summary>
    /// Dữ liệu cho form đặt lịch: khách hàng chọn thú cưng, dịch vụ và giờ hẹn.
    /// </summary>
    public class AppointmentCreateViewModel
    {
        // Phân quyền & trạng thái người dùng
        public bool IsAuthenticated { get; set; }
        public bool IsStaffOrAdmin { get; set; }
        public string? LoggedInCustomerName { get; set; }
        public string? LoggedInPhoneNumber { get; set; }

        // Khách hàng & Thú cưng (áp dụng cho thành viên đã đăng nhập hoặc Staff)
        public Guid? CustomerId { get; set; }
        public Guid? PetId { get; set; }

        // Dành cho khách vãng lai đặt lịch nhanh (Guest Booking)
        [Display(Name = "Họ và tên")]
        public string? GuestCustomerName { get; set; }

        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ (10 số).")]
        public string? GuestPhoneNumber { get; set; }

        [Display(Name = "Tên bé cưng")]
        public string? GuestPetName { get; set; }

        [Display(Name = "Loài")]
        public string? GuestSpecies { get; set; } = "Chó";

        [Display(Name = "Giống loài")]
        public string? GuestBreed { get; set; }

        [Display(Name = "Cân nặng (kg)")]
        [Range(0.1, 100, ErrorMessage = "Cân nặng từ 0.1 đến 100 kg.")]
        public decimal? GuestWeight { get; set; }

        [Display(Name = "Tiền sử dị ứng / Lưu ý")]
        public string? GuestAllergyNote { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày hẹn.")]
        [DataType(DataType.Date)]
        public DateTime? AppointmentDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu.")]
        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Giờ hẹn không hợp lệ.")]
        public string? StartTime { get; set; }

        public List<Guid> ServiceIds { get; set; } = new();

        [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
        public string? Notes { get; set; }

        // ---- Dữ liệu hiển thị trên form ----
        public List<Customer> Customers { get; set; } = new();
        public List<Service> Services { get; set; } = new();

        /// <summary>Khóa là CustomerId, dùng để đổ danh sách thú cưng theo khách hàng đã chọn.</summary>
        public Dictionary<string, List<PetOptionViewModel>> PetsByCustomer { get; set; } = new();

        public int TotalDurationMinutes { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class PetOptionViewModel
    {
        public Guid PetId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public decimal Weight { get; set; }
        public string? AllergyNote { get; set; }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(Breed)
                ? $"{PetName} ({Species})"
                : $"{PetName} ({Species} - {Breed})";
    }

    /// <summary>
    /// Một khung giờ trả về cho JavaScript khi kiểm tra lịch trống.
    /// </summary>
    public class AppointmentSlotViewModel
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool Available { get; set; }
        public string? Reason { get; set; }
    }
}
