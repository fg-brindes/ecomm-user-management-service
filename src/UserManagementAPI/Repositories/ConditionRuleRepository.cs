using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using UserManagementAPI.Models.Entities;
using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.Repositories;

public class ConditionRuleRepository : Repository<ConditionRule>, IConditionRuleRepository
{
    public ConditionRuleRepository(UserManagementDbContext context) : base(context)
    {
    }

    public async Task<List<ConditionRule>> GetRulesByConditionIdAsync(Guid conditionId)
    {
        return await _dbSet
            .Where(cr => cr.CommercialConditionId == conditionId && cr.IsActive)
            .OrderBy(cr => cr.Priority)
            .ToListAsync();
    }

    public async Task<List<ConditionRule>> GetDiscountRulesByUserIdAsync(Guid userId, Guid? companyId = null)
    {
        var query = _context.ConditionRules
            .Where(cr => cr.IsActive &&
                cr.RuleType == RuleType.Discount &&
                cr.CommercialCondition.IsActive &&
                cr.CommercialCondition.Companies.Any(ccc =>
                    ccc.IsActive &&
                    ccc.Company.CompanyUsers.Any(cu =>
                        cu.UserId == userId && cu.IsActive)));

        if (companyId.HasValue)
        {
            query = query.Where(cr =>
                cr.CommercialCondition.Companies.Any(ccc =>
                    ccc.CompanyId == companyId && ccc.IsActive));
        }

        return await query
            .OrderByDescending(cr => cr.CommercialCondition.Priority)
            .ThenBy(cr => cr.Priority)
            .ToListAsync();
    }

    public async Task<List<ConditionRule>> GetVisibilityRulesByUserIdAsync(Guid userId, Guid? companyId = null)
    {
        var query = _context.ConditionRules
            .Where(cr => cr.IsActive &&
                cr.RuleType == RuleType.Visibility &&
                cr.CommercialCondition.IsActive &&
                cr.CommercialCondition.Companies.Any(ccc =>
                    ccc.IsActive &&
                    ccc.Company.CompanyUsers.Any(cu =>
                        cu.UserId == userId && cu.IsActive)));

        if (companyId.HasValue)
        {
            query = query.Where(cr =>
                cr.CommercialCondition.Companies.Any(ccc =>
                    ccc.CompanyId == companyId && ccc.IsActive));
        }

        return await query
            .OrderByDescending(cr => cr.CommercialCondition.Priority)
            .ThenBy(cr => cr.Priority)
            .ToListAsync();
    }
}
