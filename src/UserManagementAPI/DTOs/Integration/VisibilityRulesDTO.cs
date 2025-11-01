namespace UserManagementAPI.DTOs.Integration;

public class VisibilityRulesDTO
{
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public List<VisibilityRuleDTO> Rules { get; set; } = new();
}

public class VisibilityRuleDTO
{
    public Guid RuleId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int Priority { get; set; }
}
