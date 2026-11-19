using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models.ViewModels;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Services
{
    public class PetService : IPetService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PetService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<List<PetListItemViewModel>> GetPetsAsync(Guid? customerId, string? searchTerm = null, bool isStaffOrAdmin = false)
        {
            IQueryable<Pet> query = _context.Pets
                .Include(p => p.Customer)
                .Include(p => p.Vaccinations);

            // Authorization filter
            if (!isStaffOrAdmin)
            {
                if (!customerId.HasValue)
                {
                    return new List<PetListItemViewModel>();
                }
                query = query.Where(p => p.CustomerId == customerId.Value);
            }

            // Search filter (staff can search by owner phone, owner name, or pet name)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(p =>
                    p.PetName.ToLower().Contains(term) ||
                    p.Species.ToLower().Contains(term) ||
                    (p.Breed != null && p.Breed.ToLower().Contains(term)) ||
                    p.Customer.CustomerName.ToLower().Contains(term) ||
                    p.Customer.PhoneNumber.Contains(term));
            }

            var pets = await query
                .OrderByDescending(p => p.PetId)
                .Select(p => new PetListItemViewModel
                {
                    PetId = p.PetId,
                    PetName = p.PetName,
                    Species = p.Species,
                    Breed = p.Breed,
                    Gender = p.Gender,
                    BirthDate = p.BirthDate,
                    Weight = p.Weight,
                    ImageUrl = p.ImageUrl,
                    AllergyNote = p.AllergyNote,
                    MedicalHistory = p.MedicalHistory,
                    OwnerId = p.CustomerId,
                    OwnerName = p.Customer.CustomerName,
                    OwnerPhone = p.Customer.PhoneNumber,
                    TotalVaccinations = p.Vaccinations.Count,
                    NextDueVaccineDate = p.Vaccinations
                        .Where(v => v.NextDueDate.HasValue)
                        .OrderBy(v => v.NextDueDate)
                        .Select(v => v.NextDueDate)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return pets;
        }

        public async Task<PetDetailViewModel?> GetPetDetailAsync(Guid petId, Guid? currentCustomerId, bool isStaffOrAdmin)
        {
            var pet = await _context.Pets
                .Include(p => p.Customer)
                .Include(p => p.Vaccinations)
                .Include(p => p.Appointments)
                .FirstOrDefaultAsync(p => p.PetId == petId);

            if (pet == null) return null;

            // Authorization: customer can only view their own pet
            if (!isStaffOrAdmin && currentCustomerId.HasValue && pet.CustomerId != currentCustomerId.Value)
            {
                return null;
            }

            var vaccinations = pet.Vaccinations
                .OrderByDescending(v => v.VaccinationDate)
                .Select(v => new VaccinationViewModel
                {
                    VaccinationId = v.VaccinationId,
                    PetId = v.PetId,
                    VaccineName = v.VaccineName,
                    VaccinationDate = v.VaccinationDate,
                    NextDueDate = v.NextDueDate,
                    Note = v.Note
                })
                .ToList();

            var nextDue = pet.Vaccinations
                .Where(v => v.NextDueDate.HasValue)
                .OrderBy(v => v.NextDueDate)
                .Select(v => v.NextDueDate)
                .FirstOrDefault();

            return new PetDetailViewModel
            {
                PetId = pet.PetId,
                PetName = pet.PetName,
                Species = pet.Species,
                Breed = pet.Breed,
                Gender = pet.Gender,
                BirthDate = pet.BirthDate,
                Weight = pet.Weight,
                MedicalHistory = pet.MedicalHistory,
                AllergyNote = pet.AllergyNote,
                ImageUrl = pet.ImageUrl,
                OwnerId = pet.CustomerId,
                OwnerName = pet.Customer.CustomerName,
                OwnerPhone = pet.Customer.PhoneNumber,
                OwnerEmail = pet.Customer.Email,
                OwnerAddress = pet.Customer.Address,
                Vaccinations = vaccinations,
                NextDueVaccineDate = nextDue,
                AppointmentCount = pet.Appointments.Count
            };
        }

        public async Task<PetEditViewModel?> GetPetForEditAsync(Guid petId, Guid? currentCustomerId, bool isStaffOrAdmin)
        {
            var pet = await _context.Pets.FirstOrDefaultAsync(p => p.PetId == petId);
            if (pet == null) return null;

            // Authorization check
            if (!isStaffOrAdmin && currentCustomerId.HasValue && pet.CustomerId != currentCustomerId.Value)
            {
                return null;
            }

            return new PetEditViewModel
            {
                PetId = pet.PetId,
                CustomerId = pet.CustomerId,
                PetName = pet.PetName,
                Species = pet.Species,
                Breed = pet.Breed,
                Gender = pet.Gender,
                BirthDate = pet.BirthDate,
                Weight = pet.Weight,
                MedicalHistory = pet.MedicalHistory,
                AllergyNote = pet.AllergyNote,
                ExistingImageUrl = pet.ImageUrl,
                IsStaffOrAdmin = isStaffOrAdmin
            };
        }

        public async Task<(bool Success, string? ErrorMessage, Guid PetId)> CreatePetAsync(PetCreateViewModel model, Guid customerId)
        {
            // Validation
            if (model.Weight <= 0)
            {
                return (false, "Cân nặng thú cưng phải lớn hơn 0 kg", Guid.Empty);
            }

            if (model.Gender != "Đực" && model.Gender != "Cái")
            {
                return (false, "Giới tính chỉ nhận giá trị 'Đực' hoặc 'Cái'", Guid.Empty);
            }

            var targetCustomerId = model.CustomerId ?? customerId;
            var customerExists = await _context.Customers.AnyAsync(c => c.Id == targetCustomerId);
            if (!customerExists)
            {
                return (false, "Không tìm thấy hồ sơ chủ nuôi tương ứng trong hệ thống", Guid.Empty);
            }

            // Image handling (upload file or URL)
            string? finalImageUrl = model.ImageUrl?.Trim();
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadedUrl = await SaveUploadedImageAsync(model.ImageFile);
                if (uploadedUrl != null)
                {
                    finalImageUrl = uploadedUrl;
                }
            }

            var pet = new Pet
            {
                PetId = Guid.NewGuid(),
                CustomerId = targetCustomerId,
                PetName = model.PetName.Trim(),
                Species = model.Species.Trim(),
                Breed = string.IsNullOrWhiteSpace(model.Breed) ? null : model.Breed.Trim(),
                Gender = model.Gender,
                BirthDate = model.BirthDate,
                Weight = model.Weight,
                MedicalHistory = string.IsNullOrWhiteSpace(model.MedicalHistory) ? null : model.MedicalHistory.Trim(),
                AllergyNote = string.IsNullOrWhiteSpace(model.AllergyNote) ? null : model.AllergyNote.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(finalImageUrl) ? null : finalImageUrl
            };

            _context.Pets.Add(pet);
            await _context.SaveChangesAsync();

            return (true, null, pet.PetId);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdatePetAsync(PetEditViewModel model, Guid? currentCustomerId, bool isStaffOrAdmin)
        {
            var pet = await _context.Pets.FirstOrDefaultAsync(p => p.PetId == model.PetId);
            if (pet == null)
            {
                return (false, "Không tìm thấy thú cưng cần cập nhật");
            }

            // Authorization check
            if (!isStaffOrAdmin && currentCustomerId.HasValue && pet.CustomerId != currentCustomerId.Value)
            {
                return (false, "Bạn không có quyền chỉnh sửa hồ sơ thú cưng này!");
            }

            // Validation
            if (model.Weight <= 0)
            {
                return (false, "Cân nặng phải lớn hơn 0 kg");
            }

            if (model.Gender != "Đực" && model.Gender != "Cái")
            {
                return (false, "Giới tính chỉ nhận giá trị 'Đực' hoặc 'Cái'");
            }

            // Image handling
            string? finalImageUrl = pet.ImageUrl;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploaded = await SaveUploadedImageAsync(model.ImageFile);
                if (uploaded != null) finalImageUrl = uploaded;
            }
            else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                finalImageUrl = model.ImageUrl.Trim();
            }

            // Update basic info
            pet.PetName = model.PetName.Trim();
            pet.Species = model.Species.Trim();
            pet.Breed = string.IsNullOrWhiteSpace(model.Breed) ? null : model.Breed.Trim();
            pet.Gender = model.Gender;
            pet.BirthDate = model.BirthDate;
            pet.ImageUrl = finalImageUrl;

            // Medical & Weight info update
            pet.Weight = model.Weight;
            pet.MedicalHistory = string.IsNullOrWhiteSpace(model.MedicalHistory) ? null : model.MedicalHistory.Trim();
            pet.AllergyNote = string.IsNullOrWhiteSpace(model.AllergyNote) ? null : model.AllergyNote.Trim();

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeletePetAsync(Guid petId, Guid? currentCustomerId, bool isStaffOrAdmin)
        {
            var pet = await _context.Pets
                .Include(p => p.Appointments)
                .FirstOrDefaultAsync(p => p.PetId == petId);

            if (pet == null)
            {
                return (false, "Không tìm thấy thú cưng cần xóa");
            }

            // Authorization check
            if (!isStaffOrAdmin && currentCustomerId.HasValue && pet.CustomerId != currentCustomerId.Value)
            {
                return (false, "Bạn không có quyền xóa thú cưng này!");
            }

            // Integrity check: prevent deletion if appointment/care history exists
            if (pet.Appointments != null && pet.Appointments.Any())
            {
                return (false, "Không thể xóa hồ sơ thú cưng này vì đã phát sinh lịch hẹn hoặc dịch vụ chăm sóc trong hệ thống. Để bảo toàn dữ liệu, hệ thống từ chối xóa!");
            }

            _context.Pets.Remove(pet);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> AddVaccinationAsync(VaccinationCreateViewModel model, Guid? currentCustomerId, bool isStaffOrAdmin)
        {
            var pet = await _context.Pets.FirstOrDefaultAsync(p => p.PetId == model.PetId);
            if (pet == null)
            {
                return (false, "Không tìm thấy hồ sơ thú cưng");
            }

            // Authorization check
            if (!isStaffOrAdmin && currentCustomerId.HasValue && pet.CustomerId != currentCustomerId.Value)
            {
                return (false, "Bạn không có quyền thêm mũi tiêm cho thú cưng này!");
            }

            if (string.IsNullOrWhiteSpace(model.VaccineName))
            {
                return (false, "Tên vắc-xin không được để trống");
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (model.VaccinationDate > today)
            {
                return (false, "Ngày tiêm không thể là ngày trong tương lai (tiêm rồi mới ghi nhận vào sổ)!");
            }

            if (model.NextDueDate.HasValue)
            {
                if (model.NextDueDate.Value <= model.VaccinationDate)
                {
                    return (false, "Ngày hẹn tiêm nhắc lại bắt buộc phải lớn hơn ngày tiêm chủng!");
                }
                if (model.NextDueDate.Value <= today)
                {
                    return (false, "Ngày hẹn tiêm nhắc lại bắt buộc phải là một ngày ở tương lai!");
                }
            }

            var vaccination = new Vaccination
            {
                VaccinationId = Guid.NewGuid(),
                PetId = model.PetId,
                VaccineName = model.VaccineName.Trim(),
                VaccinationDate = model.VaccinationDate,
                NextDueDate = model.NextDueDate,
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim()
            };

            _context.Vaccinations.Add(vaccination);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<List<VaccinationViewModel>> GetVaccinationsByPetAsync(Guid petId)
        {
            return await _context.Vaccinations
                .Where(v => v.PetId == petId)
                .OrderByDescending(v => v.VaccinationDate)
                .Select(v => new VaccinationViewModel
                {
                    VaccinationId = v.VaccinationId,
                    PetId = v.PetId,
                    VaccineName = v.VaccineName,
                    VaccinationDate = v.VaccinationDate,
                    NextDueDate = v.NextDueDate,
                    Note = v.Note
                })
                .ToListAsync();
        }

        public async Task<PetMedicalAlertViewModel?> GetPetMedicalAlertAsync(Guid petId)
        {
            var pet = await _context.Pets
                .Where(p => p.PetId == petId)
                .Select(p => new PetMedicalAlertViewModel
                {
                    PetId = p.PetId,
                    PetName = p.PetName,
                    Species = p.Species,
                    ActualWeight = p.Weight,
                    AllergyNote = p.AllergyNote,
                    MedicalHistory = p.MedicalHistory
                })
                .FirstOrDefaultAsync();

            return pet;
        }

        public async Task<List<CustomerOptionViewModel>> GetCustomerOptionsAsync()
        {
            return await _context.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.CustomerName)
                .Select(c => new CustomerOptionViewModel
                {
                    CustomerId = c.Id,
                    CustomerName = c.CustomerName,
                    PhoneNumber = c.PhoneNumber
                })
                .ToListAsync();
        }

        public async Task<(Guid CustomerId, Guid StaffAccountId)> EnsureDemoDataAsync()
        {
            // Seed Demo Customer Account & Customer if not exist
            var customerAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == "khachhang_demo");
            if (customerAccount == null)
            {
                customerAccount = new Account
                {
                    Id = Guid.NewGuid(),
                    Username = "khachhang_demo",
                    PasswordHash = "$2a$11$samplehashcustomer",
                    FullName = "Nguyễn Minh Anh",
                    Role = "Customer",
                    Status = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Accounts.Add(customerAccount);
                await _context.SaveChangesAsync();
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == customerAccount.Id);
            if (customer == null)
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    AccountId = customerAccount.Id,
                    CustomerName = "Nguyễn Minh Anh",
                    PhoneNumber = "0901234567",
                    Email = "minhanh@gmail.com",
                    Address = "42 Xuân Thủy, P. Thảo Điền, TP. Thủ Đức",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // Seed Demo Staff Account
            var staffAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == "nhanvien_demo");
            if (staffAccount == null)
            {
                staffAccount = new Account
                {
                    Id = Guid.NewGuid(),
                    Username = "nhanvien_demo",
                    PasswordHash = "$2a$11$samplehashstaff",
                    FullName = "Hoàng Thu Trang (Lễ tân · Ca sáng)",
                    Role = "Staff",
                    Status = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Accounts.Add(staffAccount);
                await _context.SaveChangesAsync();
            }

            // Seed Demo Pets if Customer has no pets
            var petCount = await _context.Pets.CountAsync(p => p.CustomerId == customer.Id);
            if (petCount == 0)
            {
                var pet1 = new Pet
                {
                    PetId = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    PetName = "Mochi",
                    Species = "Mèo",
                    Breed = "Mèo Anh lông ngắn",
                    Gender = "Đực",
                    BirthDate = new DateOnly(2023, 11, 14),
                    Weight = 5.60m,
                    MedicalHistory = "Bé hơi nhát, nhẹ tay khi cắt móng. Vượt mức cân gói tiêu chuẩn (<= 5 kg).",
                    AllergyNote = "Thành phần ngũ cốc, sữa tắm chứa Sulfate.",
                    ImageUrl = "https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=600&auto=format&fit=crop&q=80"
                };

                var pet2 = new Pet
                {
                    PetId = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    PetName = "Lucky",
                    Species = "Chó",
                    Breed = "Corgi Pembroke",
                    Gender = "Đực",
                    BirthDate = new DateOnly(2022, 10, 15),
                    Weight = 11.20m,
                    MedicalHistory = "Từng điều trị nấm tai, khớp hông cần theo dõi.",
                    AllergyNote = "Dị ứng đạm bò (bị ngứa da và nổi mẩn đỏ).",
                    ImageUrl = "https://images.unsplash.com/photo-1612536057832-2ff7ead7f363?w=600&auto=format&fit=crop&q=80"
                };

                _context.Pets.AddRange(pet1, pet2);
                await _context.SaveChangesAsync();

                // Seed sample vaccination
                _context.Vaccinations.AddRange(
                    new Vaccination
                    {
                        VaccinationId = Guid.NewGuid(),
                        PetId = pet1.PetId,
                        VaccineName = "Vắc-xin 4 bệnh cho Mèo (Felocell 4)",
                        VaccinationDate = new DateOnly(2024, 6, 15),
                        NextDueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)), // Sắp đến hạn
                        Note = "Tiêm mũi nhắc lại hàng năm, sức khỏe bình thường sau tiêm"
                    },
                    new Vaccination
                    {
                        VaccinationId = Guid.NewGuid(),
                        PetId = pet1.PetId,
                        VaccineName = "Vắc-xin phòng Dại (Rabisin)",
                        VaccinationDate = new DateOnly(2024, 6, 15),
                        NextDueDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)),
                        Note = "Đã cấp sổ chứng nhận tiêm dại"
                    },
                    new Vaccination
                    {
                        VaccinationId = Guid.NewGuid(),
                        PetId = pet2.PetId,
                        VaccineName = "Vắc-xin 7 bệnh cho Chó (Vanguard Plus 5/L)",
                        VaccinationDate = new DateOnly(2024, 4, 20),
                        NextDueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)), // Quá hạn
                        Note = "Cần liên hệ chủ nuôi nhắc lịch tiêm nhắc lại sớm"
                    }
                );
                await _context.SaveChangesAsync();
            }

            return (customer.Id, staffAccount.Id);
        }

        private async Task<string?> SaveUploadedImageAsync(IFormFile file)
        {
            try
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext)) return null;

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "pets");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/uploads/pets/{uniqueFileName}";
            }
            catch
            {
                return null;
            }
        }
    }
}
