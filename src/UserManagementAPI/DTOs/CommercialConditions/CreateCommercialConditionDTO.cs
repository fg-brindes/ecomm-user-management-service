using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs.CommercialConditions;

public class CreateCommercialConditionDTO
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public int Priority { get; set; } = 0;
}
