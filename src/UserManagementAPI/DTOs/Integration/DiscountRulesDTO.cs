using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.Integration;

public class DiscountRulesDTO
{
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public List<DiscountRuleDTO> Rules { get; set; } = new();
}

public class DiscountRuleDTO
{
    public Guid RuleId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public int Priority { get; set; }
}
