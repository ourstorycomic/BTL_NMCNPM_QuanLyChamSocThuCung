using System;
using System.Collections.Generic;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Invoice
    {
        public Guid InvoiceId { get; set; } = Guid.NewGuid();
        public Guid AppointmentId { get; set; }
        public decimal Surcharge { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, QRCode
        public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Paid
        public DateTime? PaymentTime { get; set; }

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}
