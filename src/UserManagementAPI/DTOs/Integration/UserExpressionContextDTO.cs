using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.Integration;

public class UserExpressionContextDTO
{
    public Guid UserId { get; set; }
    public UserType UserType { get; set; }
    public CompanyContextDTO? Company { get; set; }
}

public class CompanyContextDTO
{
    public Guid CompanyId { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
}
