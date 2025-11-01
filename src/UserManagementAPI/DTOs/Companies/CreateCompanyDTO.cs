using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs.Companies;

public class CreateCompanyDTO
{
    [Required]
    [MaxLength(20)]
    public string Cnpj { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string CorporateName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? TradeName { get; set; }

    [MaxLength(50)]
    public string? StateRegistration { get; set; }

    [MaxLength(50)]
    public string? MunicipalRegistration { get; set; }
}
