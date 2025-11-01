using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.CommercialConditions;

public class CreateConditionRuleDTO
{
    [Required]
    public RuleType RuleType { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Expression { get; set; } = string.Empty;

    // Campos obrigatórios apenas se RuleType = Discount
    public DiscountType? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int Priority { get; set; } = 0;
}
