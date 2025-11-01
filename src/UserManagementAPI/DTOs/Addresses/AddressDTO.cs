using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.DTOs.Addresses;

public class AddressDTO
{
    public Guid Id { get; set; }
    public AddressType Type { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
