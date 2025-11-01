using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.Models.Entities;

public class ConditionRule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CommercialConditionId { get; set; }

    [Required]
    public RuleType RuleType { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Expression { get; set; } = string.Empty; // Expressão que será enviada para Catalog API

    // Campos específicos para Desconto (nullable se for Visibility)
    public DiscountType? DiscountType { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? DiscountValue { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int Priority { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation Property
    [ForeignKey(nameof(CommercialConditionId))]
    public CommercialCondition CommercialCondition { get; set; } = null!;
}
