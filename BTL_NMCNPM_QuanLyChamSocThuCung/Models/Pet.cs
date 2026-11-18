using System;
using System.Collections.Generic;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Models
{
    public class Pet
    {
        public Guid PetId { get; set; } = Guid.NewGuid();
        public Guid CustomerId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public string Gender { get; set; } = string.Empty; // Đực, Cái
        public DateOnly? BirthDate { get; set; }
        public decimal Weight { get; set; }
        public string? MedicalHistory { get; set; }
        public string? AllergyNote { get; set; }

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual ICollection<Vaccination> Vaccinations { get; set; } = new List<Vaccination>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
