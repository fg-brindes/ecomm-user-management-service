using UserManagementAPI.DTOs.Addresses;
using UserManagementAPI.DTOs.Common;
using UserManagementAPI.DTOs.Companies;
using UserManagementAPI.DTOs.Users;
using UserManagementAPI.Models.Entities;
using UserManagementAPI.Repositories;

namespace UserManagementAPI.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;

    public UserService(IUserRepository userRepository, ICompanyRepository companyRepository)
    {
        _userRepository = userRepository;
        _companyRepository = companyRepository;
    }

    public async Task<UserDetailDTO?> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdWithAddressesAsync(id);
        if (user == null)
            return null;

        // Get user with companies
        var userWithCompanies = await _userRepository.GetByIdWithCompaniesAsync(id);

        var userDetailDto = new UserDetailDTO
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Document = user.Document,
            UserType = user.UserType,
            Role = user.Role,
            IsActive = user.IsActive,
            EmailVerified = user.EmailVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Addresses = user.Addresses.Select(MapToAddressDTO).ToList(),
            Companies = new List<CompanySummaryDTO>()
        };

        // Map companies if user has company associations
        if (userWithCompanies?.CompanyAssociations != null && userWithCompanies.CompanyAssociations.Any())
        {
            userDetailDto.Companies = userWithCompanies.CompanyAssociations
                .Where(ca => ca.IsActive)
                .Select(ca => new CompanySummaryDTO
                {
                    Id = ca.Company.Id,
                    Cnpj = ca.Company.Cnpj,
                    CorporateName = ca.Company.CorporateName,
                    TradeName = ca.Company.TradeName,
                    IsActive = ca.Company.IsActive
                })
                .ToList();
        }

        return userDetailDto;
    }

    public async Task<PaginatedResultDTO<UserSummaryDTO>> GetAllAsync(int page, int pageSize)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var allUsers = await _userRepository.GetAllAsync();
        var usersList = allUsers.ToList();

        var totalCount = usersList.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var skip = (page - 1) * pageSize;
        var paginatedUsers = usersList
            .Skip(skip)
            .Take(pageSize)
            .Select(MapToUserSummaryDTO)
            .ToList();

        return new PaginatedResultDTO<UserSummaryDTO>
        {
            Items = paginatedUsers,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        };
    }

    public async Task<UserDTO> CreateAsync(CreateUserDTO dto)
    {
        // Validate if email already exists
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new InvalidOperationException($"User with email '{dto.Email}' already exists.");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Document = dto.Document,
            UserType = dto.UserType,
            Role = dto.Role,
            IsActive = true,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return MapToUserDTO(createdUser);
    }

    public async Task<UserDTO?> UpdateAsync(Guid id, UpdateUserDTO dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return null;

        // Check if email is being changed and if it's already in use
        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new InvalidOperationException($"User with email '{dto.Email}' already exists.");
        }

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(dto.Name))
            user.Name = dto.Name;

        if (!string.IsNullOrWhiteSpace(dto.Email))
            user.Email = dto.Email;

        if (dto.Phone != null)
            user.Phone = dto.Phone;

        if (dto.Document != null)
            user.Document = dto.Document;

        if (dto.Role.HasValue)
            user.Role = dto.Role.Value;

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return MapToUserDTO(user);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return false;

        await _userRepository.DeleteAsync(user);
        await _userRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return false;

        if (user.IsActive)
            return true; // Already active

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return false;

        if (!user.IsActive)
            return true; // Already inactive

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return true;
    }

    public async Task<UserDTO?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
            return null;

        return MapToUserDTO(user);
    }

    // Manual DTO Mapping Methods
    private static UserDTO MapToUserDTO(User user)
    {
        return new UserDTO
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Document = user.Document,
            UserType = user.UserType,
            Role = user.Role,
            IsActive = user.IsActive,
            EmailVerified = user.EmailVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private static UserSummaryDTO MapToUserSummaryDTO(User user)
    {
        return new UserSummaryDTO
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            UserType = user.UserType,
            Role = user.Role,
            IsActive = user.IsActive
        };
    }

    private static AddressDTO MapToAddressDTO(Address address)
    {
        return new AddressDTO
        {
            Id = address.Id,
            Type = address.Type,
            PostalCode = address.PostalCode,
            Street = address.Street,
            Number = address.Number,
            Complement = address.Complement,
            Neighborhood = address.Neighborhood,
            City = address.City,
            State = address.State,
            IsDefault = address.IsDefault,
            IsActive = address.IsActive
        };
    }
}
