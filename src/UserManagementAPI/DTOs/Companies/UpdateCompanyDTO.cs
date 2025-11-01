using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs.Companies;

public class UpdateCompanyDTO
{
    [MaxLength(300)]
    public string? CorporateName { get; set; }

    [MaxLength(300)]
    public string? TradeName { get; set; }

    [MaxLength(50)]
    public string? StateRegistration { get; set; }

    [MaxLength(50)]
    public string? MunicipalRegistration { get; set; }
}
