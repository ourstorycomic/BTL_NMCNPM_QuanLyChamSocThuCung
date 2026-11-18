using System;
using System.Collections.Generic;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Appointment
    {
        public Guid AppointmentId { get; set; } = Guid.NewGuid();
        public Guid PetId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Processing, Completed, Cancelled
        public string? Notes { get; set; }

        // Navigation properties
        public virtual Pet Pet { get; set; } = null!;
        public virtual ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
        public virtual ReceptionTicket? ReceptionTicket { get; set; }
        public virtual Invoice? Invoice { get; set; }
    }
}
