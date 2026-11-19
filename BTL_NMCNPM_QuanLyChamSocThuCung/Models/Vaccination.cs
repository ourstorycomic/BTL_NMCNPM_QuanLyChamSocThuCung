using System;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Vaccination
    {
        public Guid VaccinationId { get; set; } = Guid.NewGuid();
        public Guid PetId { get; set; }
        public string VaccineName { get; set; } = string.Empty;
        public DateOnly VaccinationDate { get; set; }
        public DateOnly? NextDueDate { get; set; }
        public string? Note { get; set; }

        // Navigation properties
        public virtual Pet Pet { get; set; } = null!;
    }
}
