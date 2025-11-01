using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using UserManagementAPI.Models.Entities;

namespace UserManagementAPI.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(UserManagementDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByIdWithAddressesAsync(Guid id)
    {
        return await _dbSet
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByIdWithCompaniesAsync(Guid id)
    {
        return await _dbSet
            .Include(u => u.CompanyAssociations)
                .ThenInclude(cu => cu.Company)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }
}
