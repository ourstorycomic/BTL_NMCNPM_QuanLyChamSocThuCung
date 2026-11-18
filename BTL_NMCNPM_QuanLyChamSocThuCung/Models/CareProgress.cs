using System;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class CareProgress
    {
        public Guid ProgressId { get; set; } = Guid.NewGuid();
        public Guid TicketId { get; set; }
        public Guid StaffId { get; set; }
        public string StepDescription { get; set; } = string.Empty;
        public string ProgressStatus { get; set; } = "Processing"; // Pending, Processing, Completed
        public string? ResultImageUrl { get; set; }
        public string? StaffNotes { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ReceptionTicket ReceptionTicket { get; set; } = null!;
        public virtual Account Staff { get; set; } = null!;
    }
}
