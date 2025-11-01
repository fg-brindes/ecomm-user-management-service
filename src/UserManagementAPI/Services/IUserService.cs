using UserManagementAPI.DTOs.Common;
using UserManagementAPI.DTOs.Users;

namespace UserManagementAPI.Services;

public interface IUserService
{
    Task<UserDetailDTO?> GetByIdAsync(Guid id);
    Task<PaginatedResultDTO<UserSummaryDTO>> GetAllAsync(int page, int pageSize);
    Task<UserDTO> CreateAsync(CreateUserDTO dto);
    Task<UserDTO?> UpdateAsync(Guid id, UpdateUserDTO dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ActivateAsync(Guid id);
    Task<bool> DeactivateAsync(Guid id);
    Task<UserDTO?> GetByEmailAsync(string email);
}
