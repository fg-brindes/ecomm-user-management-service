using UserManagementAPI.Models.Entities;

namespace UserManagementAPI.Repositories;

public interface ICommercialConditionRepository : IRepository<CommercialCondition>
{
    Task<CommercialCondition?> GetByIdWithRulesAsync(Guid id);
    Task<List<CommercialCondition>> GetActiveConditionsAsync();
    Task<List<CommercialCondition>> GetConditionsByCompanyIdAsync(Guid companyId);
    Task<List<CommercialCondition>> GetConditionsByUserIdAsync(Guid userId);
}
