using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using UserManagementAPI.Models.Entities;

namespace UserManagementAPI.Repositories;

public class CommercialConditionRepository : Repository<CommercialCondition>, ICommercialConditionRepository
{
    public CommercialConditionRepository(UserManagementDbContext context) : base(context)
    {
    }

    public async Task<CommercialCondition?> GetByIdWithRulesAsync(Guid id)
    {
        return await _dbSet
            .Include(cc => cc.Rules)
            .FirstOrDefaultAsync(cc => cc.Id == id);
    }

    public async Task<List<CommercialCondition>> GetActiveConditionsAsync()
    {
        return await _dbSet
            .Where(cc => cc.IsActive)
            .OrderByDescending(cc => cc.Priority)
            .ThenBy(cc => cc.Name)
            .ToListAsync();
    }

    public async Task<List<CommercialCondition>> GetConditionsByCompanyIdAsync(Guid companyId)
    {
        return await _dbSet
            .Where(cc => cc.Companies.Any(ccc => ccc.CompanyId == companyId && ccc.IsActive))
            .Include(cc => cc.Rules)
            .OrderByDescending(cc => cc.Priority)
            .ThenBy(cc => cc.Name)
            .ToListAsync();
    }

    public async Task<List<CommercialCondition>> GetConditionsByUserIdAsync(Guid userId)
    {
        return await _context.CommercialConditions
            .Where(cc => cc.Companies.Any(ccc =>
                ccc.Company.CompanyUsers.Any(cu =>
                    cu.UserId == userId && cu.IsActive) && ccc.IsActive))
            .Include(cc => cc.Rules)
            .OrderByDescending(cc => cc.Priority)
            .ThenBy(cc => cc.Name)
            .ToListAsync();
    }
}
