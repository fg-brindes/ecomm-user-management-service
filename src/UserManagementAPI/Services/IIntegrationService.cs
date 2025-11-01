using UserManagementAPI.DTOs.Integration;

namespace UserManagementAPI.Services;

public interface IIntegrationService
{
    Task<UserCommercialConditionsDTO> GetUserCommercialConditionsAsync(Guid userId);
    Task<UserCommercialConditionsDTO> GetCompanyCommercialConditionsAsync(Guid companyId);
    Task<VisibilityRulesDTO> GetVisibilityRulesAsync(Guid userId, Guid? companyId);
    Task<DiscountRulesDTO> GetDiscountRulesAsync(Guid userId, Guid? companyId);
    Task<UserExpressionContextDTO?> GetUserExpressionContextAsync(Guid userId);
    Task<AccessCheckDTO?> GetAccessCheckAsync(Guid userId);
}
