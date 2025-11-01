using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs.Companies;

public class AssociateUserDTO
{
    [Required]
    public Guid UserId { get; set; }

    public bool IsAdministrator { get; set; } = false;
}
