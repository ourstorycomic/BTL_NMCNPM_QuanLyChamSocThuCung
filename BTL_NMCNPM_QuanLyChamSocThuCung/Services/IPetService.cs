using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models.ViewModels;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Services
{
    public interface IPetService
    {
        Task<List<PetListItemViewModel>> GetPetsAsync(Guid? customerId, string? searchTerm = null, bool isStaffOrAdmin = false);
        Task<PetDetailViewModel?> GetPetDetailAsync(Guid petId, Guid? currentCustomerId, bool isStaffOrAdmin);
        Task<PetEditViewModel?> GetPetForEditAsync(Guid petId, Guid? currentCustomerId, bool isStaffOrAdmin);
        Task<(bool Success, string? ErrorMessage, Guid PetId)> CreatePetAsync(PetCreateViewModel model, Guid customerId);
        Task<(bool Success, string? ErrorMessage)> UpdatePetAsync(PetEditViewModel model, Guid? currentCustomerId, bool isStaffOrAdmin);
        Task<(bool Success, string? ErrorMessage)> DeletePetAsync(Guid petId, Guid? currentCustomerId, bool isStaffOrAdmin);
        Task<(bool Success, string? ErrorMessage)> AddVaccinationAsync(VaccinationCreateViewModel model, Guid? currentCustomerId, bool isStaffOrAdmin);
        Task<List<VaccinationViewModel>> GetVaccinationsByPetAsync(Guid petId);
        Task<PetMedicalAlertViewModel?> GetPetMedicalAlertAsync(Guid petId);
        Task<List<CustomerOptionViewModel>> GetCustomerOptionsAsync();
        Task<(Guid CustomerId, Guid StaffAccountId)> EnsureDemoDataAsync();
    }
}
