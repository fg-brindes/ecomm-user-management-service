using UserManagementAPI.Models.Entities;

namespace UserManagementAPI.Repositories;

public interface IConditionRuleRepository : IRepository<ConditionRule>
{
    Task<List<ConditionRule>> GetRulesByConditionIdAsync(Guid conditionId);
    Task<List<ConditionRule>> GetDiscountRulesByUserIdAsync(Guid userId, Guid? companyId = null);
    Task<List<ConditionRule>> GetVisibilityRulesByUserIdAsync(Guid userId, Guid? companyId = null);
}
