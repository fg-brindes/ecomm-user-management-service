using UserManagementAPI.DTOs.CommercialConditions;

namespace UserManagementAPI.DTOs.Integration;

public class CommercialConditionWithRulesDTO
{
    public Guid ConditionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int Priority { get; set; }
    public List<ConditionRuleDTO> Rules { get; set; } = new();
}
