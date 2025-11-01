using UserManagementAPI.Models.Entities;

namespace UserManagementAPI.Repositories;

public interface ICompanyRepository : IRepository<Company>
{
    Task<Company?> GetByCnpjAsync(string cnpj);
    Task<Company?> GetByIdWithUsersAsync(Guid id);
    Task<Company?> GetByIdWithAddressesAsync(Guid id);
    Task<Company?> GetByIdWithConditionsAsync(Guid id);
    Task<List<Company>> GetActiveCompaniesAsync();
}
