using System;
using System.ComponentModel.DataAnnotations;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Account
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string PasswordHash { get; set; }

        public string Role { get; set; } = "Customer";

        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property (Quan hệ 1-1)
        public Customer Customer { get; set; }
    }
}