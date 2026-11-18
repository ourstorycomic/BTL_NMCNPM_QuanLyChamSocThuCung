using System;
using System.Collections.Generic;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Customer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? AccountId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Account? Account { get; set; }
        public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
    }
}
