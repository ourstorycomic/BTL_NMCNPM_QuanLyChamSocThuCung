using System;
using System.Collections.Generic;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class ReceptionTicket
    {
        public Guid TicketId { get; set; } = Guid.NewGuid();
        public Guid AppointmentId { get; set; }
        public decimal ActualWeight { get; set; }
        public Guid StaffId { get; set; }
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual Account Staff { get; set; } = null!;
        public virtual ICollection<CareProgress> CareProgresses { get; set; } = new List<CareProgress>();
    }
}
