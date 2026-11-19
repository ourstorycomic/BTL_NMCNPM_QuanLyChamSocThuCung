using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using Microsoft.EntityFrameworkCore;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Data;

public static class ReceptionSeedData
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        // Seed một nhân viên mẫu nếu CSDL chưa có nhân viên hoạt động.
        var staff = await db.Accounts.FirstOrDefaultAsync(a => a.Role == "Staff" && a.Status);
        if (staff == null)
        {
            staff = new Account
            {
                Username = "staff.demo",
                // Chỉ là dữ liệu mẫu; chưa dùng để đăng nhập. Khi có chức năng tài khoản,
                // hãy thay bằng BCrypt hash được tạo từ quy trình đăng ký/tạo tài khoản.
                PasswordHash = "SEED_ONLY_NOT_A_LOGIN_HASH",
                FullName = "Nhân viên tiếp nhận mẫu",
                Role = "Staff",
                Status = true
            };
            db.Accounts.Add(staff);
        }

        if (!await db.Services.AnyAsync())
        {
            db.Services.AddRange(
                new Service { ServiceName = "Tắm vệ sinh", Category = "Vệ sinh", Description = "Tắm và vệ sinh cơ bản", DurationMinutes = 60, BasePrice = 150000m, WeightTierMin = 0m, WeightTierMax = 20m },
                new Service { ServiceName = "Cắt tỉa lông", Category = "Làm đẹp", Description = "Cắt tỉa và tạo kiểu lông", DurationMinutes = 90, BasePrice = 200000m, WeightTierMin = 0m, WeightTierMax = 30m },
                new Service { ServiceName = "Kiểm tra sức khỏe", Category = "Sức khỏe", Description = "Kiểm tra tình trạng tổng quát", DurationMinutes = 30, BasePrice = 100000m, WeightTierMin = 0m, WeightTierMax = 50m }
            );
        }

        if (!await db.Customers.AnyAsync())
        {
            db.Customers.Add(new Customer
            {
                CustomerName = "Nguyễn Minh Anh",
                PhoneNumber = "0912345678",
                Email = "minhanh.demo@example.com",
                Address = "Hà Nội",
                IsActive = true
            });
        }

        await db.SaveChangesAsync();

        if (!await db.Pets.AnyAsync())
        {
            var customer = await db.Customers.OrderBy(c => c.CreatedAt).FirstAsync();
            db.Pets.Add(new Pet
            {
                CustomerId = customer.Id,
                PetName = "Milu",
                Species = "Chó",
                Breed = "Poodle",
                Gender = "Đực",
                BirthDate = new DateOnly(2023, 5, 10),
                Weight = 5.5m,
                MedicalHistory = "Chưa ghi nhận bệnh lý.",
                AllergyNote = "Chưa ghi nhận dị ứng."
            });
        }

        await db.SaveChangesAsync();

        if (!await db.Appointments.AnyAsync())
        {
            var pet = await db.Pets.OrderBy(p => p.PetName).FirstAsync();
            var service = await db.Services.OrderBy(s => s.ServiceName).FirstOrDefaultAsync();
            var appointment = new Appointment
            {
                PetId = pet.PetId,
                AppointmentDate = DateTime.Today.AddDays(1).AddHours(9),
                EndTime = DateTime.Today.AddDays(1).AddHours(10),
                Status = "Confirmed",
                Notes = "Lịch hẹn mẫu để kiểm tra chức năng tiếp nhận."
            };
            if (service != null)
                appointment.AppointmentServices.Add(new AppointmentService { ServiceId = service.ServiceId, Quantity = 1 });
            db.Appointments.Add(appointment);
        }

        await db.SaveChangesAsync();
    }
}