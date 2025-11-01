using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.CommercialConditions;

public class UpdateConditionRuleDTO
{
    [MaxLength(2000)]
    public string? Expression { get; set; }

    public DiscountType? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? Priority { get; set; }
}
