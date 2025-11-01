using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.Users;

public class UserSummaryDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserType UserType { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}
