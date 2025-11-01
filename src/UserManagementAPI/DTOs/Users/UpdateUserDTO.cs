using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.Users;

public class UpdateUserDTO
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? Document { get; set; }

    public UserRole? Role { get; set; }
}
