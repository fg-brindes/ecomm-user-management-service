using UserManagementAPI.DTOs.CommercialConditions;
using UserManagementAPI.DTOs.Common;

namespace UserManagementAPI.Services;

public interface ICommercialConditionService
{
    Task<CommercialConditionDetailDTO?> GetByIdAsync(Guid id);
    Task<PaginatedResultDTO<CommercialConditionDTO>> GetAllAsync(int page, int pageSize);
    Task<CommercialConditionDTO> CreateAsync(CreateCommercialConditionDTO dto);
    Task<CommercialConditionDTO?> UpdateAsync(Guid id, UpdateCommercialConditionDTO dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ActivateAsync(Guid id);
    Task<bool> DeactivateAsync(Guid id);
    Task<ConditionRuleDTO> CreateRuleAsync(Guid conditionId, CreateConditionRuleDTO dto);
    Task<ConditionRuleDTO?> UpdateRuleAsync(Guid conditionId, Guid ruleId, UpdateConditionRuleDTO dto);
    Task<bool> DeleteRuleAsync(Guid conditionId, Guid ruleId);
    Task<List<ConditionRuleDTO>> GetRulesByConditionIdAsync(Guid conditionId);
    Task<bool> AssignToCompanyAsync(Guid companyId, Guid conditionId);
    Task<bool> UnassignFromCompanyAsync(Guid companyId, Guid conditionId);
}
