using System;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class AppointmentService
    {
        public Guid AppointmentId { get; set; }
        public Guid ServiceId { get; set; }
        public int Quantity { get; set; } = 1;

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
    }
}
