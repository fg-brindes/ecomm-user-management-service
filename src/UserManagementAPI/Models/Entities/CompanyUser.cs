using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementAPI.Models.Entities;

public class CompanyUser
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public bool IsAdministrator { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime AssociatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DisassociatedAt { get; set; }

    public Guid? AssociatedByUserId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
