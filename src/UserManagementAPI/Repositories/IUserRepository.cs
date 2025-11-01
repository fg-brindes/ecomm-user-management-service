using UserManagementAPI.Models.Entities;

namespace UserManagementAPI.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdWithAddressesAsync(Guid id);
    Task<User?> GetByIdWithCompaniesAsync(Guid id);
    Task<List<User>> GetActiveUsersAsync();
}
