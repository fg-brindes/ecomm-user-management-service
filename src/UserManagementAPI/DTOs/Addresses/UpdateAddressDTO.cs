using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.Addresses;

public class UpdateAddressDTO
{
    public AddressType? Type { get; set; }

    [MaxLength(10)]
    public string? PostalCode { get; set; }

    [MaxLength(200)]
    public string? Street { get; set; }

    [MaxLength(20)]
    public string? Number { get; set; }

    [MaxLength(100)]
    public string? Complement { get; set; }

    [MaxLength(100)]
    public string? Neighborhood { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? State { get; set; }

    public bool? IsDefault { get; set; }
}
