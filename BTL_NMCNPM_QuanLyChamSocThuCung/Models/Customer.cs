using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Customer
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Khóa ngoại liên kết tới bảng Account
        public Guid AccountId { get; set; }

        [ForeignKey("AccountId")]
        public Account Account { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string CustomerName { get; set; }

        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public string Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}