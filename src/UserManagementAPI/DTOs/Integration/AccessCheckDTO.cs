namespace UserManagementAPI.DTOs.Integration;

public class AccessCheckDTO
{
    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
    public bool HasCompany { get; set; }
    public bool CompanyIsActive { get; set; }
    public bool HasActiveConditions { get; set; }
}
