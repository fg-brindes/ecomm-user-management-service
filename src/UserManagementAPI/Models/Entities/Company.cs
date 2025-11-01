using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models.Entities;

public class Company
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(20)]
    public string Cnpj { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string CorporateName { get; set; } = string.Empty; // Razão Social

    [MaxLength(300)]
    public string? TradeName { get; set; } // Nome Fantasia

    [MaxLength(50)]
    public string? StateRegistration { get; set; } // Inscrição Estadual

    [MaxLength(50)]
    public string? MunicipalRegistration { get; set; } // Inscrição Municipal

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    // Navigation Properties
    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    public ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();

    public ICollection<CompanyCommercialCondition> CommercialConditions { get; set; }
        = new List<CompanyCommercialCondition>();
}
