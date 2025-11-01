using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using UserManagementAPI.Models.Entities;

namespace UserManagementAPI.Repositories;

public class CompanyRepository : Repository<Company>, ICompanyRepository
{
    public CompanyRepository(UserManagementDbContext context) : base(context)
    {
    }

    public async Task<Company?> GetByCnpjAsync(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(c => c.Cnpj == cnpj);
    }

    public async Task<Company?> GetByIdWithUsersAsync(Guid id)
    {
        return await _dbSet
            .Include(c => c.CompanyUsers)
                .ThenInclude(cu => cu.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company?> GetByIdWithAddressesAsync(Guid id)
    {
        return await _dbSet
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company?> GetByIdWithConditionsAsync(Guid id)
    {
        return await _dbSet
            .Include(c => c.CommercialConditions)
                .ThenInclude(cc => cc.CommercialCondition)
                    .ThenInclude(c => c.Rules)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Company>> GetActiveCompaniesAsync()
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.CorporateName)
            .ToListAsync();
    }
}
