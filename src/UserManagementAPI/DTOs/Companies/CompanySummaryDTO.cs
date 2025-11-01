namespace UserManagementAPI.DTOs.Companies;

public class CompanySummaryDTO
{
    public Guid Id { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string CorporateName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public bool IsActive { get; set; }
}
