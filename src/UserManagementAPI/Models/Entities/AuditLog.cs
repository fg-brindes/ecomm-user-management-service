using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementAPI.Models.Entities;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty; // User, Company, CommercialCondition, etc.

    [Required]
    public Guid EntityId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Activate, Deactivate

    [Column(TypeName = "jsonb")]
    public string? Changes { get; set; } // JSON com as mudanças

    public Guid? PerformedByUserId { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
