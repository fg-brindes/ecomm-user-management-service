using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs.CommercialConditions;

public class UpdateCommercialConditionDTO
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public int? Priority { get; set; }
}
