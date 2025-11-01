using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.CommercialConditions;

public class ConditionRuleDTO
{
    public Guid Id { get; set; }
    public Guid CommercialConditionId { get; set; }
    public RuleType RuleType { get; set; }
    public string Expression { get; set; } = string.Empty;
    public DiscountType? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public string? Description { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
