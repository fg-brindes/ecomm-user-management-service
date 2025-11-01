using UserManagementAPI.DTOs.Addresses;
using UserManagementAPI.DTOs.Users;

namespace UserManagementAPI.DTOs.Companies;

public class CompanyDetailDTO
{
    public Guid Id { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string CorporateName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? StateRegistration { get; set; }
    public string? MunicipalRegistration { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<AddressDTO> Addresses { get; set; } = new();
    public List<UserSummaryDTO> Users { get; set; } = new();
}
