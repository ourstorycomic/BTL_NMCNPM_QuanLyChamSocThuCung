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
        [Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
        public Guid? CustomerId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thú cưng.")]
        public Guid? PetId { get; set; }

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
