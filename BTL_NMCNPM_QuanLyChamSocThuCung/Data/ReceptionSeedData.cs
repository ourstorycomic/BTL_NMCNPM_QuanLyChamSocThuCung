using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using Microsoft.EntityFrameworkCore;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Data;

public static class ReceptionSeedData
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");

        // 1. Seed tài khoản Admin (Quản trị viên)
        var admin = await db.Accounts.FirstOrDefaultAsync(a => a.Username == "admin");
        if (admin == null)
        {
            admin = new Account
            {
                Username = "admin",
                PasswordHash = defaultPasswordHash,
                FullName = "Quản trị viên hệ thống",
                Role = "Admin",
                Status = true
            };
            db.Accounts.Add(admin);
        }
        else
        {
            admin.PasswordHash = defaultPasswordHash;
            admin.Status = true;
        }

        // 2. Seed tài khoản Staff (Lễ tân & Kỹ thuật viên)
        var staff = await db.Accounts.FirstOrDefaultAsync(a => a.Username == "letan");
        if (staff == null)
        {
            staff = new Account
            {
                Username = "letan",
                PasswordHash = defaultPasswordHash,
                FullName = "Hoàng Thu Trang",
                Role = "Staff",
                Status = true
            };
            db.Accounts.Add(staff);
        }
        else
        {
            staff.PasswordHash = defaultPasswordHash;
            staff.Status = true;
        }

        // Kỹ thuật viên Groomer Trần Đức Huy
        var groomer = await db.Accounts.FirstOrDefaultAsync(a => a.Username == "groomer.huy");
        if (groomer == null)
        {
            groomer = new Account
            {
                Username = "groomer.huy",
                PasswordHash = defaultPasswordHash,
                FullName = "Trần Đức Huy",
                Role = "Staff",
                Status = true
            };
            db.Accounts.Add(groomer);
        }
        else
        {
            groomer.PasswordHash = defaultPasswordHash;
            groomer.Status = true;
        }

        // 3. Seed tài khoản Khách hàng (Nguyễn Minh Anh)
        var custAcc = await db.Accounts.FirstOrDefaultAsync(a => a.Username == "minhanh");
        if (custAcc == null)
        {
            custAcc = new Account
            {
                Username = "minhanh",
                PasswordHash = defaultPasswordHash,
                FullName = "Nguyễn Minh Anh",
                Role = "Customer",
                Status = true
            };
            db.Accounts.Add(custAcc);
        }
        else
        {
            custAcc.PasswordHash = defaultPasswordHash;
            custAcc.Status = true;
        }

        await db.SaveChangesAsync();

        // 4. Seed Khách hàng Hồ sơ
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == "0901234567");
        if (customer == null)
        {
            customer = new Customer
            {
                AccountId = custAcc.Id,
                CustomerName = "Nguyễn Minh Anh",
                PhoneNumber = "0901234567",
                Email = "minhanh@gmail.com",
                Address = "42 Xuân Thủy, Thảo Điền, TP. Thủ Đức",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
        }
        else if (customer.AccountId == null)
        {
            customer.AccountId = custAcc.Id;
            await db.SaveChangesAsync();
        }

        // Khách hàng phụ: Trần Quốc Bảo
        if (!await db.Customers.AnyAsync(c => c.PhoneNumber == "0912345678"))
        {
            db.Customers.Add(new Customer
            {
                CustomerName = "Trần Quốc Bảo",
                PhoneNumber = "0912345678",
                Email = "quocbao@gmail.com",
                Address = "Quận 1, TP. Hồ Chí Minh",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // 5. Seed Danh mục Dịch vụ chuẩn 6 dịch vụ
        var services = await db.Services.ToListAsync();
        if (services.Count < 6)
        {
            var requiredServices = new List<Service>
            {
                new() { ServiceName = "Tắm vệ sinh", Category = "Vệ sinh", Description = "Tắm sấy, chải lông, vắt tuyến hôi", DurationMinutes = 60, BasePrice = 150000m, WeightTierMin = 0m, WeightTierMax = 30m, IsActive = true },
                new() { ServiceName = "Cắt tỉa lông", Category = "Làm đẹp", Description = "Tạo kiểu nghệ thuật theo yêu cầu", DurationMinutes = 90, BasePrice = 250000m, WeightTierMin = 0m, WeightTierMax = 30m, IsActive = true },
                new() { ServiceName = "Khách sạn thú cưng", Category = "Lưu trú", Description = "Phòng riêng máy lạnh, camera 24/7", DurationMinutes = 1440, BasePrice = 200000m, WeightTierMin = 0m, WeightTierMax = 50m, IsActive = true },
                new() { ServiceName = "Vệ sinh tai & cắt móng", Category = "Chăm sóc", Description = "Làm sạch tai sinh học, mài móng êm", DurationMinutes = 20, BasePrice = 80000m, WeightTierMin = 0m, WeightTierMax = 30m, IsActive = true },
                new() { ServiceName = "Khám sức khỏe tổng quát", Category = "Y tế", Description = "Bác sĩ thú y thăm khám tim phổi da", DurationMinutes = 30, BasePrice = 200000m, WeightTierMin = 0m, WeightTierMax = 50m, IsActive = true },
                new() { ServiceName = "Spa thư giãn thảo mộc", Category = "Spa", Description = "Massage tinh dầu thảo mộc phục hồi lông", DurationMinutes = 45, BasePrice = 350000m, WeightTierMin = 0m, WeightTierMax = 30m, IsActive = true }
            };

            foreach (var req in requiredServices)
            {
                if (!services.Any(s => s.ServiceName == req.ServiceName))
                {
                    db.Services.Add(req);
                }
            }
            await db.SaveChangesAsync();
            services = await db.Services.ToListAsync();
        }

        // 6. Seed Thú cưng Mochi
        var mochi = await db.Pets.FirstOrDefaultAsync(p => p.CustomerId == customer.Id && p.PetName == "Mochi");
        if (mochi == null)
        {
            mochi = new Pet
            {
                CustomerId = customer.Id,
                PetName = "Mochi",
                Species = "Mèo",
                Breed = "Mèo Anh lông ngắn",
                Gender = "Đực",
                BirthDate = new DateOnly(2023, 3, 15),
                Weight = 5.6m,
                MedicalHistory = "Khỏe mạnh, đã tiêm ngừa 3 mũi",
                AllergyNote = "Thành phần ngũ cốc, sữa tắm chứa Sulfate. Bé hơi nhát người lạ.",
                ImageUrl = "https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=200&auto=format&fit=crop&q=80"
            };
            db.Pets.Add(mochi);
            await db.SaveChangesAsync();
        }

        // Bé cún Lucky của khách Bảo
        var custBao = await db.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == "0912345678");
        if (custBao != null && !await db.Pets.AnyAsync(p => p.CustomerId == custBao.Id && p.PetName == "Lucky"))
        {
            db.Pets.Add(new Pet
            {
                CustomerId = custBao.Id,
                PetName = "Lucky",
                Species = "Chó",
                Breed = "Poodle",
                Gender = "Đực",
                BirthDate = new DateOnly(2023, 8, 20),
                Weight = 4.5m,
                MedicalHistory = "Tiêm ngừa đầy đủ",
                AllergyNote = "Không ghi nhận",
                ImageUrl = "https://images.unsplash.com/photo-1543466835-00a7907e9de1?w=200&auto=format&fit=crop&q=80"
            });
            await db.SaveChangesAsync();
        }

        // 7. Seed Lịch hẹn chờ tiếp nhận (Confirmed) cho bé Mochi hôm nay
        var apptConfirmed = await db.Appointments
            .FirstOrDefaultAsync(a => a.PetId == mochi.PetId && a.Status == "Confirmed");

        if (apptConfirmed == null)
        {
            var svcs = services.Take(2).ToList();
            apptConfirmed = new Appointment
            {
                PetId = mochi.PetId,
                AppointmentDate = DateTime.Today.AddHours(10),
                EndTime = DateTime.Today.AddHours(11).AddMinutes(20),
                Status = "Confirmed",
                Notes = "Bé hơi nhát người lạ, xin nhẹ nhàng khi cắt móng."
            };
            foreach (var s in svcs)
            {
                apptConfirmed.AppointmentServices.Add(new AppointmentService
                {
                    ServiceId = s.ServiceId,
                    Quantity = 1
                });
            }
            db.Appointments.Add(apptConfirmed);
            await db.SaveChangesAsync();
        }

        // 8. Seed Cuộc hẹn đã tiếp nhận (Processing) sẵn sàng vào thanh toán hóa đơn
        var apptProcessing = await db.Appointments
            .Include(a => a.ReceptionTicket)
            .FirstOrDefaultAsync(a => a.PetId == mochi.PetId && a.Status == "Processing");

        if (apptProcessing == null)
        {
            apptProcessing = new Appointment
            {
                PetId = mochi.PetId,
                AppointmentDate = DateTime.Today.AddHours(8).AddMinutes(30),
                EndTime = DateTime.Today.AddHours(9).AddMinutes(50),
                Status = "Processing",
                Notes = "Mochi vượt mức cân gói tiêu chuẩn (< 5 kg). Bé hơi cáu khi cắt móng."
            };
            foreach (var s in services.Take(3))
            {
                apptProcessing.AppointmentServices.Add(new AppointmentService
                {
                    ServiceId = s.ServiceId,
                    Quantity = 1
                });
            }
            db.Appointments.Add(apptProcessing);
            await db.SaveChangesAsync();

            var ticket = new ReceptionTicket
            {
                AppointmentId = apptProcessing.AppointmentId,
                ActualWeight = 5.6m,
                StaffId = groomer.Id,
                CheckInTime = DateTime.Now.AddHours(-1)
            };
            db.ReceptionTickets.Add(ticket);
            await db.SaveChangesAsync();

            db.CareProgresses.Add(new CareProgress
            {
                TicketId = ticket.TicketId,
                StaffId = groomer.Id,
                StepDescription = "Grooming & Vệ sinh tai móng",
                ProgressStatus = "Processing",
                StaffNotes = "Đã tắm bằng sữa tắm không Sulfate. Phát hiện ráy tai nhẹ bên trái, đã vệ sinh.",
                UpdatedAt = DateTime.Now
            });
            await db.SaveChangesAsync();
        }
    }
}
