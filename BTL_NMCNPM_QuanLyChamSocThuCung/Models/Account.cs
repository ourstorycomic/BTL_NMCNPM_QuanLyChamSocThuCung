using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Giữ lại để dùng [Key], [Required]

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Account
    {
        [Key] // Giữ lại để định nghĩa khóa chính
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")] // Giữ lại để validate
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")] // Giữ lại để validate
        public string PasswordHash { get; set; } = string.Empty;

        // Thuộc tính mới của nhóm (dev)
        public string FullName { get; set; } = string.Empty;
        
        public string Role { get; set; } = string.Empty; // Admin, Staff, Customer
        
        public bool Status { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (Lấy theo nhánh dev vì nhóm đã thiết kế thêm)
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<ReceptionTicket> ReceptionTickets { get; set; } = new List<ReceptionTicket>();
        public virtual ICollection<CareProgress> CareProgresses { get; set; } = new List<CareProgress>();
    }
}