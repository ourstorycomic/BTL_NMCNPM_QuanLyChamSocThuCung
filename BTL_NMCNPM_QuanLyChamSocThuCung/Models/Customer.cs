using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Customer
    {
        [Key] // Giữ lại để định nghĩa khóa chính
        public Guid Id { get; set; } = Guid.NewGuid();

        // Khóa ngoại liên kết tới bảng Account
        public Guid? AccountId { get; set; }

        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string CustomerName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public string? Address { get; set; }
        
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (Lấy từ nhánh dev)
        public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
    }
}