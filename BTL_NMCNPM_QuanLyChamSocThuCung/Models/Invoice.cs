using System;
using System.Collections.Generic;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Invoice
    {
        public Guid InvoiceId { get; set; } = Guid.NewGuid();
        public Guid AppointmentId { get; set; }
        public decimal Surcharge { get; set; } = 0;
        public string? SurchargeReason { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public string? DiscountCode { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, QRCode
        public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Paid
        public DateTime? PaymentTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}
