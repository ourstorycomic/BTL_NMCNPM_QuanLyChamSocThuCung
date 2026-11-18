using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models.ViewModels
{
    public class PetListItemViewModel
    {
        public Guid PetId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public string Gender { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public decimal Weight { get; set; }
        public string? ImageUrl { get; set; }
        public string? AllergyNote { get; set; }
        public string? MedicalHistory { get; set; }

        public bool HasMedicalAlert => !string.IsNullOrWhiteSpace(AllergyNote) || !string.IsNullOrWhiteSpace(MedicalHistory);

        // Owner info
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerPhone { get; set; } = string.Empty;

        // Vaccination summary
        public DateOnly? NextDueVaccineDate { get; set; }
        public int TotalVaccinations { get; set; }

        public string AgeDisplay
        {
            get
            {
                if (!BirthDate.HasValue) return "Chưa rõ";
                var today = DateOnly.FromDateTime(DateTime.Today);
                int months = (today.Year - BirthDate.Value.Year) * 12 + today.Month - BirthDate.Value.Month;
                if (today.Day < BirthDate.Value.Day) months--;
                if (months < 0) return "Chưa sinh";
                if (months < 12) return $"{months} tháng";
                int years = months / 12;
                int remMonths = months % 12;
                return remMonths > 0 ? $"{years} tuổi {remMonths} tháng" : $"{years} tuổi";
            }
        }
    }

    public class PetDetailViewModel
    {
        public Guid PetId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public string Gender { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public decimal Weight { get; set; }
        public string? MedicalHistory { get; set; }
        public string? AllergyNote { get; set; }
        public string? ImageUrl { get; set; }

        public bool HasMedicalAlert => !string.IsNullOrWhiteSpace(AllergyNote) || !string.IsNullOrWhiteSpace(MedicalHistory);

        // Owner Info
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerPhone { get; set; } = string.Empty;
        public string? OwnerEmail { get; set; }
        public string? OwnerAddress { get; set; }

        // Vaccination list
        public List<VaccinationViewModel> Vaccinations { get; set; } = new List<VaccinationViewModel>();
        public DateOnly? NextDueVaccineDate { get; set; }

        // Constraint check info
        public int AppointmentCount { get; set; }
        public bool CanDelete => AppointmentCount == 0;

        public string AgeDisplay
        {
            get
            {
                if (!BirthDate.HasValue) return "Chưa rõ";
                var today = DateOnly.FromDateTime(DateTime.Today);
                int months = (today.Year - BirthDate.Value.Year) * 12 + today.Month - BirthDate.Value.Month;
                if (today.Day < BirthDate.Value.Day) months--;
                if (months < 0) return "Chưa sinh";
                if (months < 12) return $"{months} tháng tuổi";
                int years = months / 12;
                int remMonths = months % 12;
                return remMonths > 0 ? $"{years} tuổi {remMonths} tháng" : $"{years} tuổi";
            }
        }
    }

    public class PetCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên thú cưng")]
        [StringLength(100, ErrorMessage = "Tên không quá 100 ký tự")]
        [Display(Name = "Tên thú cưng")]
        public string PetName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn hoặc nhập loài thú cưng")]
        [StringLength(50, ErrorMessage = "Loài không quá 50 ký tự")]
        [Display(Name = "Loài (Chó, Mèo...)")]
        public string Species { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Giống không quá 100 ký tự")]
        [Display(Name = "Giống loài (Poodle, Corgi, Ba Tư...)")]
        public string? Breed { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        [RegularExpression("^(Đực|Cái)$", ErrorMessage = "Giới tính chỉ nhận giá trị 'Đực' hoặc 'Cái'")]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; } = "Đực";

        [Display(Name = "Ngày sinh ước lượng")]
        [DataType(DataType.Date)]
        public DateOnly? BirthDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập cân nặng")]
        [Range(0.01, 999.99, ErrorMessage = "Cân nặng phải lớn hơn 0 kg")]
        [Display(Name = "Cân nặng (kg)")]
        public decimal Weight { get; set; }

        [Display(Name = "Tiền sử bệnh nền & Lưu ý sức khỏe")]
        public string? MedicalHistory { get; set; }

        [StringLength(255, ErrorMessage = "Ghi chú dị ứng tối đa 255 ký tự")]
        [Display(Name = "Ghi chú dị ứng (Thuốc, thức ăn, phấn hoa...)")]
        public string? AllergyNote { get; set; }

        [Display(Name = "Đường dẫn ảnh đại diện (URL)")]
        [Url(ErrorMessage = "Đường dẫn URL không hợp lệ")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Hoặc tải ảnh từ máy tính")]
        public IFormFile? ImageFile { get; set; }

        // Owner assignment (For Staff creating pet for customer, or preset for Customer)
        [Display(Name = "Chủ nuôi")]
        public Guid? CustomerId { get; set; }
    }

    public class PetEditViewModel
    {
        [Required]
        public Guid PetId { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên thú cưng")]
        [StringLength(100, ErrorMessage = "Tên không quá 100 ký tự")]
        [Display(Name = "Tên thú cưng")]
        public string PetName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loài thú cưng")]
        [StringLength(50, ErrorMessage = "Loài không quá 50 ký tự")]
        [Display(Name = "Loài")]
        public string Species { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Giống không quá 100 ký tự")]
        [Display(Name = "Giống loài")]
        public string? Breed { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        [RegularExpression("^(Đực|Cái)$", ErrorMessage = "Giới tính chỉ nhận giá trị 'Đực' hoặc 'Cái'")]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; } = "Đực";

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateOnly? BirthDate { get; set; }

        [Required(ErrorMessage = "Cân nặng phải lớn hơn 0")]
        [Range(0.01, 999.99, ErrorMessage = "Cân nặng phải lớn hơn 0 kg")]
        [Display(Name = "Cân nặng hiện tại (kg)")]
        public decimal Weight { get; set; }

        [Display(Name = "Tiền sử bệnh nền")]
        public string? MedicalHistory { get; set; }

        [StringLength(255, ErrorMessage = "Ghi chú dị ứng tối đa 255 ký tự")]
        [Display(Name = "Ghi chú dị ứng")]
        public string? AllergyNote { get; set; }

        public string? ExistingImageUrl { get; set; }

        [Display(Name = "Cập nhật đường dẫn ảnh (URL)")]
        [Url(ErrorMessage = "Đường dẫn URL không hợp lệ")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Hoặc tải ảnh mới từ máy")]
        public IFormFile? ImageFile { get; set; }

        // Role indicator
        public bool IsStaffOrAdmin { get; set; }
    }

    public class VaccinationViewModel
    {
        public Guid VaccinationId { get; set; }
        public Guid PetId { get; set; }
        public string VaccineName { get; set; } = string.Empty;
        public DateOnly VaccinationDate { get; set; }
        public DateOnly? NextDueDate { get; set; }
        public string? Note { get; set; }

        public string StatusBadgeClass
        {
            get
            {
                if (!NextDueDate.HasValue) return "bg-secondary";
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (NextDueDate.Value < today) return "bg-danger"; // Quá hạn
                if (NextDueDate.Value <= today.AddDays(14)) return "bg-warning text-dark"; // Sắp đến hạn
                return "bg-success"; // Còn hạn
            }
        }

        public string StatusText
        {
            get
            {
                if (!NextDueDate.HasValue) return "Không có lịch hẹn";
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (NextDueDate.Value < today) return $"Quá hạn ({NextDueDate.Value:dd/MM/yyyy})";
                if (NextDueDate.Value <= today.AddDays(14)) return $"Sắp đến hẹn ({NextDueDate.Value:dd/MM/yyyy})";
                return $"Đến hẹn: {NextDueDate.Value:dd/MM/yyyy}";
            }
        }
    }

    public class VaccinationCreateViewModel
    {
        [Required]
        public Guid PetId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên vắc-xin")]
        [StringLength(100, ErrorMessage = "Tên vắc-xin không quá 100 ký tự")]
        [Display(Name = "Tên vắc-xin")]
        public string VaccineName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày tiêm")]
        [Display(Name = "Ngày tiêm chủng")]
        [DataType(DataType.Date)]
        public DateOnly VaccinationDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Display(Name = "Ngày hẹn tiêm nhắc lại")]
        [DataType(DataType.Date)]
        public DateOnly? NextDueDate { get; set; }

        [StringLength(255, ErrorMessage = "Ghi chú không quá 255 ký tự")]
        [Display(Name = "Ghi chú lần tiêm (Liều lượng, phản ứng sau tiêm...)")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO cung cấp cho UC07 (Đặt lịch hẹn) và UC09 (Tiếp nhận tại quầy)
    /// </summary>
    public class PetMedicalAlertViewModel
    {
        public Guid PetId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public decimal ActualWeight { get; set; }
        public string? AllergyNote { get; set; }
        public string? MedicalHistory { get; set; }
        public bool HasAlert => !string.IsNullOrWhiteSpace(AllergyNote) || !string.IsNullOrWhiteSpace(MedicalHistory);
        public string AlertSummary
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(AllergyNote)) parts.Add($"[Dị ứng: {AllergyNote}]");
                if (!string.IsNullOrWhiteSpace(MedicalHistory)) parts.Add($"[Bệnh nền: {MedicalHistory}]");
                return string.Join(" - ", parts);
            }
        }
    }

    public class CustomerOptionViewModel
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string DisplayText => $"{CustomerName} ({PhoneNumber})";
    }
}
