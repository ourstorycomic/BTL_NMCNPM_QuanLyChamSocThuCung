using System;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class InvoiceDetail
    {
        public Guid InvoiceDetailId { get; set; } = Guid.NewGuid();
        public Guid InvoiceId { get; set; }
        public Guid ServiceId { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }

        // Navigation properties
        public virtual Invoice Invoice { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
    }
}
