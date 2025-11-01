using UserManagementAPI.DTOs.Addresses;
using UserManagementAPI.DTOs.Common;
using UserManagementAPI.DTOs.Companies;
using UserManagementAPI.DTOs.Users;
using UserManagementAPI.Models.Entities;
using UserManagementAPI.Repositories;

namespace UserManagementAPI.Services;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<CompanyUser> _companyUserRepository;

    public CompanyService(
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        IRepository<CompanyUser> companyUserRepository)
    {
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _companyUserRepository = companyUserRepository;
    }

    public async Task<CompanyDetailDTO?> GetByIdAsync(Guid id)
    {
        var company = await _companyRepository.GetByIdWithAddressesAsync(id);
        if (company == null)
            return null;

        // Get company with users
        var companyWithUsers = await _companyRepository.GetByIdWithUsersAsync(id);

        var companyDetailDto = new CompanyDetailDTO
        {
            Id = company.Id,
            Cnpj = company.Cnpj,
            CorporateName = company.CorporateName,
            TradeName = company.TradeName,
            StateRegistration = company.StateRegistration,
            MunicipalRegistration = company.MunicipalRegistration,
            IsActive = company.IsActive,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt,
            Addresses = company.Addresses.Select(MapToAddressDTO).ToList(),
            Users = new List<UserSummaryDTO>()
        };

        // Map users if company has user associations
        if (companyWithUsers?.CompanyUsers != null && companyWithUsers.CompanyUsers.Any())
        {
            companyDetailDto.Users = companyWithUsers.CompanyUsers
                .Where(cu => cu.IsActive)
                .Select(cu => new UserSummaryDTO
                {
                    Id = cu.User.Id,
                    Name = cu.User.Name,
                    Email = cu.User.Email,
                    UserType = cu.User.UserType,
                    Role = cu.User.Role,
                    IsActive = cu.User.IsActive
                })
                .ToList();
        }

        return companyDetailDto;
    }

    public async Task<PaginatedResultDTO<CompanyDTO>> GetAllAsync(int page, int pageSize)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var allCompanies = await _companyRepository.GetAllAsync();
        var companiesList = allCompanies.ToList();

        var totalCount = companiesList.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var skip = (page - 1) * pageSize;
        var paginatedCompanies = companiesList
            .Skip(skip)
            .Take(pageSize)
            .Select(MapToCompanyDTO)
            .ToList();

        return new PaginatedResultDTO<CompanyDTO>
        {
            Items = paginatedCompanies,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        };
    }

    public async Task<CompanyDTO> CreateAsync(CreateCompanyDTO dto)
    {
        // Validate if CNPJ already exists
        var existingCompany = await _companyRepository.GetByCnpjAsync(dto.Cnpj);
        if (existingCompany != null)
            throw new InvalidOperationException($"Company with CNPJ '{dto.Cnpj}' already exists.");

        var company = new Company
        {
            Cnpj = dto.Cnpj,
            CorporateName = dto.CorporateName,
            TradeName = dto.TradeName,
            StateRegistration = dto.StateRegistration,
            MunicipalRegistration = dto.MunicipalRegistration,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdCompany = await _companyRepository.AddAsync(company);
        await _companyRepository.SaveChangesAsync();

        return MapToCompanyDTO(createdCompany);
    }

    public async Task<CompanyDTO?> UpdateAsync(Guid id, UpdateCompanyDTO dto)
    {
        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null)
            return null;

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(dto.CorporateName))
            company.CorporateName = dto.CorporateName;

        if (dto.TradeName != null)
            company.TradeName = dto.TradeName;

        if (dto.StateRegistration != null)
            company.StateRegistration = dto.StateRegistration;

        if (dto.MunicipalRegistration != null)
            company.MunicipalRegistration = dto.MunicipalRegistration;

        company.UpdatedAt = DateTime.UtcNow;

        await _companyRepository.UpdateAsync(company);
        await _companyRepository.SaveChangesAsync();

        return MapToCompanyDTO(company);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null)
            return false;

        await _companyRepository.DeleteAsync(company);
        await _companyRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null)
            return false;

        if (company.IsActive)
            return true; // Already active

        company.IsActive = true;
        company.UpdatedAt = DateTime.UtcNow;

        await _companyRepository.UpdateAsync(company);
        await _companyRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null)
            return false;

        if (!company.IsActive)
            return true; // Already inactive

        company.IsActive = false;
        company.UpdatedAt = DateTime.UtcNow;

        await _companyRepository.UpdateAsync(company);
        await _companyRepository.SaveChangesAsync();

        return true;
    }

    public async Task<CompanyDTO?> GetByCnpjAsync(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return null;

        var company = await _companyRepository.GetByCnpjAsync(cnpj);
        if (company == null)
            return null;

        return MapToCompanyDTO(company);
    }

    public async Task<bool> AssociateUserAsync(Guid companyId, AssociateUserDTO dto)
    {
        // Validate company exists
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null)
            throw new InvalidOperationException($"Company with ID '{companyId}' not found.");

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(dto.UserId);
        if (user == null)
            throw new InvalidOperationException($"User with ID '{dto.UserId}' not found.");

        // Check if association already exists
        var existingAssociations = await _companyUserRepository.FindAsync(
            cu => cu.CompanyId == companyId && cu.UserId == dto.UserId);

        var existingAssociation = existingAssociations.FirstOrDefault();

        if (existingAssociation != null)
        {
            // If association exists but is inactive, reactivate it
            if (!existingAssociation.IsActive)
            {
                existingAssociation.IsActive = true;
                existingAssociation.AssociatedAt = DateTime.UtcNow;
                existingAssociation.DisassociatedAt = null;
                existingAssociation.IsAdministrator = dto.IsAdministrator;

                await _companyUserRepository.UpdateAsync(existingAssociation);
                await _companyUserRepository.SaveChangesAsync();
                return true;
            }

            // Association already active
            return true;
        }

        // Create new association
        var companyUser = new CompanyUser
        {
            CompanyId = companyId,
            UserId = dto.UserId,
            IsAdministrator = dto.IsAdministrator,
            IsActive = true,
            AssociatedAt = DateTime.UtcNow
        };

        await _companyUserRepository.AddAsync(companyUser);
        await _companyUserRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DisassociateUserAsync(Guid companyId, Guid userId)
    {
        // Validate company exists
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null)
            return false;

        // Find the association
        var associations = await _companyUserRepository.FindAsync(
            cu => cu.CompanyId == companyId && cu.UserId == userId && cu.IsActive);

        var association = associations.FirstOrDefault();
        if (association == null)
            return false;

        // Deactivate the association
        association.IsActive = false;
        association.DisassociatedAt = DateTime.UtcNow;

        await _companyUserRepository.UpdateAsync(association);
        await _companyUserRepository.SaveChangesAsync();

        return true;
    }

    // Manual DTO Mapping Methods
    private static CompanyDTO MapToCompanyDTO(Company company)
    {
        return new CompanyDTO
        {
            Id = company.Id,
            Cnpj = company.Cnpj,
            CorporateName = company.CorporateName,
            TradeName = company.TradeName,
            StateRegistration = company.StateRegistration,
            MunicipalRegistration = company.MunicipalRegistration,
            IsActive = company.IsActive,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt
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
