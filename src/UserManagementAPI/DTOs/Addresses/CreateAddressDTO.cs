using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.Addresses;

public class CreateAddressDTO
{
    [Required]
    public AddressType Type { get; set; }

    [Required]
    [MaxLength(10)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Number { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Complement { get; set; }

    [Required]
    [MaxLength(100)]
    public string Neighborhood { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(2)]
    public string State { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;
}
