using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models.Entities;

public class CommercialCondition
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public int Priority { get; set; } = 0; // Maior = mais prioritário

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    // Navigation Properties
    public ICollection<CompanyCommercialCondition> Companies { get; set; }
        = new List<CompanyCommercialCondition>();

    public ICollection<ConditionRule> Rules { get; set; } = new List<ConditionRule>();
}
