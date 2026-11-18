using System;
using System.Collections.Generic;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Service
    {
        public Guid ServiceId { get; set; } = Guid.NewGuid();
        public string ServiceName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public decimal WeightTierMin { get; set; } = 0;
        public decimal WeightTierMax { get; set; } = 999.99m;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}
