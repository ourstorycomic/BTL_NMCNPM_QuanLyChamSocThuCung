using Microsoft.EntityFrameworkCore;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Pet> Pets => Set<Pet>();
        public DbSet<Vaccination> Vaccinations => Set<Vaccination>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<AppointmentService> AppointmentServices => Set<AppointmentService>();
        public DbSet<ReceptionTicket> ReceptionTickets => Set<ReceptionTicket>();
        public DbSet<CareProgress> CareProgresses => Set<CareProgress>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 1. Account
            // ==========================================
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("Account", t =>
                {
                    t.HasCheckConstraint("CK_Account_Role", "[Role] IN ('Admin', 'Staff', 'Customer')");
                });

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Username).HasMaxLength(50).IsUnicode(true).IsRequired();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsUnicode(false).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(100).IsUnicode(true).IsRequired();
                entity.Property(e => e.Role).HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.Property(e => e.Status).HasDefaultValue(true).IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()").IsRequired();
            });

            // ==========================================
            // 2. Customer
            // ==========================================
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customer");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.CustomerName).HasMaxLength(100).IsUnicode(true).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(15).IsUnicode(false).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(254).IsUnicode(true);
                entity.Property(e => e.Address).HasMaxLength(255).IsUnicode(true);
                entity.Property(e => e.IsActive).HasDefaultValue(true).IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()").IsRequired();

                entity.HasIndex(e => e.AccountId).IsUnique().HasFilter("[AccountId] IS NOT NULL");

                entity.HasOne(e => e.Account)
                      .WithOne(a => a.Customer)
                      .HasForeignKey<Customer>(e => e.AccountId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ==========================================
            // 3. Pet
            // ==========================================
            modelBuilder.Entity<Pet>(entity =>
            {
                entity.ToTable("Pet", t =>
                {
                    t.HasCheckConstraint("CK_Pet_Gender", "[Gender] IN (N'Đực', N'Cái')");
                    t.HasCheckConstraint("CK_Pet_Weight", "[Weight] > 0");
                });

                entity.HasKey(e => e.PetId);
                entity.Property(e => e.PetId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.PetName).HasMaxLength(100).IsUnicode(true).IsRequired();
                entity.Property(e => e.Species).HasMaxLength(50).IsUnicode(true).IsRequired();
                entity.Property(e => e.Breed).HasMaxLength(100).IsUnicode(true);
                entity.Property(e => e.Gender).HasMaxLength(10).IsUnicode(true).IsRequired();
                entity.Property(e => e.BirthDate).HasColumnType("date");
                entity.Property(e => e.Weight).HasPrecision(5, 2).IsRequired();
                entity.Property(e => e.MedicalHistory).HasColumnType("nvarchar(max)");
                entity.Property(e => e.AllergyNote).HasMaxLength(255).IsUnicode(true);
                entity.Property(e => e.ImageUrl).HasMaxLength(508).IsUnicode(false);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Pets)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 4. Vaccination
            // ==========================================
            modelBuilder.Entity<Vaccination>(entity =>
            {
                entity.ToTable("Vaccination");

                entity.HasKey(e => e.VaccinationId);
                entity.Property(e => e.VaccinationId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.VaccineName).HasMaxLength(100).IsUnicode(true).IsRequired();
                entity.Property(e => e.VaccinationDate).HasColumnType("date").IsRequired();
                entity.Property(e => e.NextDueDate).HasColumnType("date");
                entity.Property(e => e.Note).HasMaxLength(255).IsUnicode(true);

                entity.HasOne(e => e.Pet)
                      .WithMany(p => p.Vaccinations)
                      .HasForeignKey(e => e.PetId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 5. Service
            // ==========================================
            modelBuilder.Entity<Service>(entity =>
            {
                entity.ToTable("Service", t =>
                {
                    t.HasCheckConstraint("CK_Service_BasePrice", "[BasePrice] >= 0");
                    t.HasCheckConstraint("CK_Service_WeightTierMin", "[WeightTierMin] >= 0");
                    t.HasCheckConstraint("CK_Service_WeightTierMax", "[WeightTierMax] >= [WeightTierMin]");
                    t.HasCheckConstraint("CK_Service_DurationMinutes", "[DurationMinutes] > 0");
                });

                entity.HasKey(e => e.ServiceId);
                entity.Property(e => e.ServiceId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.ServiceName).HasMaxLength(100).IsUnicode(true).IsRequired();
                entity.Property(e => e.Category).HasMaxLength(50).IsUnicode(true).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(255).IsUnicode(true);
                entity.Property(e => e.DurationMinutes).HasDefaultValue(30).IsRequired();
                entity.Property(e => e.BasePrice).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.WeightTierMin).HasPrecision(5, 2).HasDefaultValue(0m).IsRequired();
                entity.Property(e => e.WeightTierMax).HasPrecision(5, 2).HasDefaultValue(999.99m).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true).IsRequired();
            });

            // ==========================================
            // 6. Appointment
            // ==========================================
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointment", t =>
                {
                    t.HasCheckConstraint("CK_Appointment_Status", "[Status] IN ('Pending', 'Confirmed', 'Processing', 'Completed', 'Cancelled')");
                });

                entity.HasKey(e => e.AppointmentId);
                entity.Property(e => e.AppointmentId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.AppointmentDate).HasColumnType("datetime2").IsRequired();
                entity.Property(e => e.EndTime).HasColumnType("datetime2");
                entity.Property(e => e.Status).HasMaxLength(30).IsUnicode(false).HasDefaultValue("Pending").IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(255).IsUnicode(true);

                entity.HasOne(e => e.Pet)
                      .WithMany(p => p.Appointments)
                      .HasForeignKey(e => e.PetId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 7. AppointmentService
            // ==========================================
            modelBuilder.Entity<AppointmentService>(entity =>
            {
                entity.ToTable("AppointmentService", t =>
                {
                    t.HasCheckConstraint("CK_AppointmentService_Quantity", "[Quantity] > 0");
                });

                entity.HasKey(e => new { e.AppointmentId, e.ServiceId });
                entity.Property(e => e.Quantity).HasDefaultValue(1).IsRequired();

                entity.HasOne(e => e.Appointment)
                      .WithMany(a => a.AppointmentServices)
                      .HasForeignKey(e => e.AppointmentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Service)
                      .WithMany(s => s.AppointmentServices)
                      .HasForeignKey(e => e.ServiceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 8. ReceptionTicket
            // ==========================================
            modelBuilder.Entity<ReceptionTicket>(entity =>
            {
                entity.ToTable("ReceptionTicket", t =>
                {
                    t.HasCheckConstraint("CK_ReceptionTicket_ActualWeight", "[ActualWeight] > 0");
                });

                entity.HasKey(e => e.TicketId);
                entity.Property(e => e.TicketId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.ActualWeight).HasPrecision(5, 2).IsRequired();
                entity.Property(e => e.CheckInTime).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()").IsRequired();

                entity.HasIndex(e => e.AppointmentId).IsUnique();

                entity.HasOne(e => e.Appointment)
                      .WithOne(a => a.ReceptionTicket)
                      .HasForeignKey<ReceptionTicket>(e => e.AppointmentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Staff)
                      .WithMany(a => a.ReceptionTickets)
                      .HasForeignKey(e => e.StaffId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 9. CareProgress
            // ==========================================
            modelBuilder.Entity<CareProgress>(entity =>
            {
                entity.ToTable("CareProgress", t =>
                {
                    t.HasCheckConstraint("CK_CareProgress_ProgressStatus", "[ProgressStatus] IN ('Pending', 'Processing', 'Completed')");
                });

                entity.HasKey(e => e.ProgressId);
                entity.Property(e => e.ProgressId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.StepDescription).HasMaxLength(255).IsUnicode(true).IsRequired();
                entity.Property(e => e.ProgressStatus).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Processing").IsRequired();
                entity.Property(e => e.ResultImageUrl).HasMaxLength(508).IsUnicode(false);
                entity.Property(e => e.StaffNotes).HasColumnType("nvarchar(max)");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()").IsRequired();

                entity.HasOne(e => e.ReceptionTicket)
                      .WithMany(rt => rt.CareProgresses)
                      .HasForeignKey(e => e.TicketId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Staff)
                      .WithMany(a => a.CareProgresses)
                      .HasForeignKey(e => e.StaffId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 10. Invoice
            // ==========================================
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.ToTable("Invoice", t =>
                {
                    t.HasCheckConstraint("CK_Invoice_Surcharge", "[Surcharge] >= 0");
                    t.HasCheckConstraint("CK_Invoice_DiscountAmount", "[DiscountAmount] >= 0");
                    t.HasCheckConstraint("CK_Invoice_TotalAmount", "[TotalAmount] >= 0");
                    t.HasCheckConstraint("CK_Invoice_PaymentMethod", "[PaymentMethod] IN ('Cash', 'QRCode')");
                    t.HasCheckConstraint("CK_Invoice_PaymentStatus", "[PaymentStatus] IN ('Unpaid', 'Paid')");
                });

                entity.HasKey(e => e.InvoiceId);
                entity.Property(e => e.InvoiceId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Surcharge).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
                entity.Property(e => e.SurchargeReason).HasMaxLength(255).IsUnicode(true);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
                entity.Property(e => e.DiscountCode).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.PaymentMethod).HasMaxLength(30).IsUnicode(false).IsRequired();
                entity.Property(e => e.PaymentStatus).HasMaxLength(30).IsUnicode(false).HasDefaultValue("Unpaid").IsRequired();
                entity.Property(e => e.PaymentTime).HasColumnType("datetime2");
                entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()").IsRequired();

                entity.HasIndex(e => e.AppointmentId).IsUnique();

                entity.HasOne(e => e.Appointment)
                      .WithOne(a => a.Invoice)
                      .HasForeignKey<Invoice>(e => e.AppointmentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 11. InvoiceDetail
            // ==========================================
            modelBuilder.Entity<InvoiceDetail>(entity =>
            {
                entity.ToTable("InvoiceDetail", t =>
                {
                    t.HasCheckConstraint("CK_InvoiceDetail_Quantity", "[Quantity] > 0");
                    t.HasCheckConstraint("CK_InvoiceDetail_UnitPrice", "[UnitPrice] >= 0");
                    t.HasCheckConstraint("CK_InvoiceDetail_Amount", "[Amount] >= 0");
                });

                entity.HasKey(e => e.InvoiceDetailId);
                entity.Property(e => e.InvoiceDetailId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Quantity).HasDefaultValue(1).IsRequired();
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();

                entity.HasOne(e => e.Invoice)
                      .WithMany(i => i.InvoiceDetails)
                      .HasForeignKey(e => e.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Service)
                      .WithMany(s => s.InvoiceDetails)
                      .HasForeignKey(e => e.ServiceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
