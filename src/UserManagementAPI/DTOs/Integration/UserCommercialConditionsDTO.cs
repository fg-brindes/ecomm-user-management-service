using UserManagementAPI.DTOs.CommercialConditions;

namespace UserManagementAPI.DTOs.Integration;

public class UserCommercialConditionsDTO
{
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public List<CommercialConditionWithRulesDTO> Conditions { get; set; } = new();
}
