using UserManagementAPI.DTOs.Common;
using UserManagementAPI.DTOs.Companies;

namespace UserManagementAPI.Services;

public interface ICompanyService
{
    Task<CompanyDetailDTO?> GetByIdAsync(Guid id);
    Task<PaginatedResultDTO<CompanyDTO>> GetAllAsync(int page, int pageSize);
    Task<CompanyDTO> CreateAsync(CreateCompanyDTO dto);
    Task<CompanyDTO?> UpdateAsync(Guid id, UpdateCompanyDTO dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ActivateAsync(Guid id);
    Task<bool> DeactivateAsync(Guid id);
    Task<CompanyDTO?> GetByCnpjAsync(string cnpj);
    Task<bool> AssociateUserAsync(Guid companyId, AssociateUserDTO dto);
    Task<bool> DisassociateUserAsync(Guid companyId, Guid userId);
}
