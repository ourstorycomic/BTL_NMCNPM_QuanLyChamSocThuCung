using System;
using System.Collections.Generic;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Account
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Admin, Staff, Customer
        public bool Status { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<ReceptionTicket> ReceptionTickets { get; set; } = new List<ReceptionTicket>();
        public virtual ICollection<CareProgress> CareProgresses { get; set; } = new List<CareProgress>();
    }
}
