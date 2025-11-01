using UserManagementAPI.DTOs.Addresses;
using UserManagementAPI.DTOs.Companies;
using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.Users;

public class UserDetailDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Document { get; set; }
    public UserType UserType { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<AddressDTO> Addresses { get; set; } = new();
    public List<CompanySummaryDTO> Companies { get; set; } = new();
}
