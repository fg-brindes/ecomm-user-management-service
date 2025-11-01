using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.DTOs.Common;
using UserManagementAPI.DTOs.Companies;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

/// <summary>
/// Controller for managing company accounts and user associations.
/// </summary>
/// <remarks>
/// This controller provides endpoints for CRUD operations on companies,
/// including activation/deactivation, CNPJ-based lookups, and user associations.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Companies")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly ILogger<CompaniesController> _logger;

    /// <summary>
    /// Initializes a new instance of the CompaniesController.
    /// </summary>
    /// <param name="companyService">The company service for business logic operations.</param>
    /// <param name="logger">The logger instance for diagnostic information.</param>
    public CompaniesController(ICompanyService companyService, ILogger<CompaniesController> logger)
    {
        _companyService = companyService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of companies.
    /// </summary>
    /// <param name="page">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 10).</param>
    /// <returns>A paginated list of companies.</returns>
    /// <response code="200">Returns the paginated list of companies.</response>
    /// <response code="400">If the pagination parameters are invalid.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/companies?page=1&amp;pageSize=10
    ///
    /// The response includes pagination metadata and company information.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResultDTO<CompanyDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResultDTO<CompanyDTO>>> GetCompanies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Retrieving companies - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            if (page < 1 || pageSize < 1)
            {
                return BadRequest(new { error = "Page and pageSize must be greater than 0." });
            }

            var result = await _companyService.GetAllAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving companies list");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves detailed information about a specific company.
    /// </summary>
    /// <param name="id">The unique identifier of the company.</param>
    /// <returns>Detailed company information including related data.</returns>
    /// <response code="200">Returns the company details.</response>
    /// <response code="404">If the company is not found.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/companies/550e8400-e29b-41d4-a716-446655440000
    ///
    /// Returns comprehensive company information including addresses, associated users,
    /// and commercial conditions.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompanyDetailDTO>> GetCompanyById(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving company by ID: {CompanyId}", id);

            var company = await _companyService.GetByIdAsync(id);

            if (company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", id);
                return NotFound(new { error = $"Company with ID {id} not found." });
            }

            return Ok(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company: {CompanyId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new company account.
    /// </summary>
    /// <param name="dto">The company creation data transfer object.</param>
    /// <returns>The newly created company.</returns>
    /// <response code="201">Returns the newly created company.</response>
    /// <response code="400">If the company data is invalid or creation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/companies
    ///     {
    ///         "cnpj": "12345678000190",
    ///         "tradeName": "ABC Corporation",
    ///         "legalName": "ABC Corporation Ltd",
    ///         "stateRegistration": "123456789",
    ///         "municipalRegistration": "987654321",
    ///         "phone": "+5511999999999",
    ///         "email": "contact@abc.com",
    ///         "website": "https://www.abc.com",
    ///         "addresses": [
    ///             {
    ///                 "street": "Business Ave",
    ///                 "number": "1000",
    ///                 "city": "São Paulo",
    ///                 "state": "SP",
    ///                 "zipCode": "01234567",
    ///                 "country": "Brazil",
    ///                 "addressType": "Commercial"
    ///             }
    ///         ]
    ///     }
    ///
    /// CNPJ must be unique and valid according to Brazilian tax ID rules.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(CompanyDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompanyDTO>> CreateCompany([FromBody] CreateCompanyDTO dto)
    {
        try
        {
            _logger.LogInformation("Creating new company with CNPJ: {Cnpj}", dto.Cnpj);

            var company = await _companyService.CreateAsync(dto);

            _logger.LogInformation("Company created successfully with ID: {CompanyId}", company.Id);
            return CreatedAtAction(nameof(GetCompanyById), new { id = company.Id }, company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating company");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing company's information.
    /// </summary>
    /// <param name="id">The unique identifier of the company to update.</param>
    /// <param name="dto">The company update data transfer object.</param>
    /// <returns>The updated company information.</returns>
    /// <response code="200">Returns the updated company.</response>
    /// <response code="404">If the company is not found.</response>
    /// <response code="400">If the update data is invalid or update fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/companies/550e8400-e29b-41d4-a716-446655440000
    ///     {
    ///         "tradeName": "ABC Corporation Updated",
    ///         "phone": "+5511888888888",
    ///         "email": "newcontact@abc.com"
    ///     }
    ///
    /// Only provided fields will be updated. CNPJ cannot be changed through this endpoint.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CompanyDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompanyDTO>> UpdateCompany(Guid id, [FromBody] UpdateCompanyDTO dto)
    {
        try
        {
            _logger.LogInformation("Updating company: {CompanyId}", id);

            var company = await _companyService.UpdateAsync(id, dto);

            if (company == null)
            {
                _logger.LogWarning("Company not found for update: {CompanyId}", id);
                return NotFound(new { error = $"Company with ID {id} not found." });
            }

            _logger.LogInformation("Company updated successfully: {CompanyId}", id);
            return Ok(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company: {CompanyId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a company account.
    /// </summary>
    /// <param name="id">The unique identifier of the company to delete.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">Company successfully deleted.</response>
    /// <response code="404">If the company is not found.</response>
    /// <response code="400">If deletion fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /api/companies/550e8400-e29b-41d4-a716-446655440000
    ///
    /// This operation performs a soft delete by setting the IsDeleted flag.
    /// Company data is retained for audit purposes. All associated users will be
    /// automatically disassociated from the company.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting company: {CompanyId}", id);

            var result = await _companyService.DeleteAsync(id);

            if (!result)
            {
                _logger.LogWarning("Company not found for deletion: {CompanyId}", id);
                return NotFound(new { error = $"Company with ID {id} not found." });
            }

            _logger.LogInformation("Company deleted successfully: {CompanyId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting company: {CompanyId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Activates a company account.
    /// </summary>
    /// <param name="id">The unique identifier of the company to activate.</param>
    /// <returns>No content on successful activation.</returns>
    /// <response code="204">Company successfully activated.</response>
    /// <response code="404">If the company is not found.</response>
    /// <response code="400">If activation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/companies/550e8400-e29b-41d4-a716-446655440000/activate
    ///
    /// Activating a company enables its users to access the system and apply
    /// associated commercial conditions to their transactions.
    /// </remarks>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateCompany(Guid id)
    {
        try
        {
            _logger.LogInformation("Activating company: {CompanyId}", id);

            var result = await _companyService.ActivateAsync(id);

            if (!result)
            {
                _logger.LogWarning("Company not found for activation: {CompanyId}", id);
                return NotFound(new { error = $"Company with ID {id} not found." });
            }

            _logger.LogInformation("Company activated successfully: {CompanyId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating company: {CompanyId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deactivates a company account.
    /// </summary>
    /// <param name="id">The unique identifier of the company to deactivate.</param>
    /// <returns>No content on successful deactivation.</returns>
    /// <response code="204">Company successfully deactivated.</response>
    /// <response code="404">If the company is not found.</response>
    /// <response code="400">If deactivation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/companies/550e8400-e29b-41d4-a716-446655440000/deactivate
    ///
    /// Deactivating a company prevents its users from applying the company's
    /// commercial conditions while retaining the account data. The account can be
    /// reactivated later.
    /// </remarks>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateCompany(Guid id)
    {
        try
        {
            _logger.LogInformation("Deactivating company: {CompanyId}", id);

            var result = await _companyService.DeactivateAsync(id);

            if (!result)
            {
                _logger.LogWarning("Company not found for deactivation: {CompanyId}", id);
                return NotFound(new { error = $"Company with ID {id} not found." });
            }

            _logger.LogInformation("Company deactivated successfully: {CompanyId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating company: {CompanyId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a company by its CNPJ (Brazilian tax identification number).
    /// </summary>
    /// <param name="cnpj">The CNPJ of the company (14 digits, with or without formatting).</param>
    /// <returns>The company information.</returns>
    /// <response code="200">Returns the company details.</response>
    /// <response code="404">If no company is found with the specified CNPJ.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/companies/cnpj/12345678000190
    ///
    /// CNPJ can be provided with or without formatting (dots, slashes, hyphens).
    /// </remarks>
    [HttpGet("cnpj/{cnpj}")]
    [ProducesResponseType(typeof(CompanyDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompanyDTO>> GetCompanyByCnpj(string cnpj)
    {
        try
        {
            _logger.LogInformation("Retrieving company by CNPJ: {Cnpj}", cnpj);

            var company = await _companyService.GetByCnpjAsync(cnpj);

            if (company == null)
            {
                _logger.LogWarning("Company not found with CNPJ: {Cnpj}", cnpj);
                return NotFound(new { error = $"Company with CNPJ {cnpj} not found." });
            }

            return Ok(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company by CNPJ: {Cnpj}", cnpj);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Associates a user with a company.
    /// </summary>
    /// <param name="id">The unique identifier of the company.</param>
    /// <param name="dto">The user association data transfer object.</param>
    /// <returns>No content on successful association.</returns>
    /// <response code="204">User successfully associated with the company.</response>
    /// <response code="404">If the company or user is not found.</response>
    /// <response code="400">If the association data is invalid or association fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/companies/550e8400-e29b-41d4-a716-446655440000/users
    ///     {
    ///         "userId": "660e8400-e29b-41d4-a716-446655440000"
    ///     }
    ///
    /// A user can be associated with multiple companies. The association enables
    /// the user to access company-specific commercial conditions and data.
    /// </remarks>
    [HttpPost("{id:guid}/users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssociateUser(Guid id, [FromBody] AssociateUserDTO dto)
    {
        try
        {
            _logger.LogInformation("Associating user {UserId} with company {CompanyId}", dto.UserId, id);

            var result = await _companyService.AssociateUserAsync(id, dto);

            if (!result)
            {
                _logger.LogWarning("Failed to associate user {UserId} with company {CompanyId}", dto.UserId, id);
                return NotFound(new { error = $"Company with ID {id} or User not found." });
            }

            _logger.LogInformation("User {UserId} associated successfully with company {CompanyId}", dto.UserId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error associating user {UserId} with company {CompanyId}", dto.UserId, id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Disassociates a user from a company.
    /// </summary>
    /// <param name="id">The unique identifier of the company.</param>
    /// <param name="userId">The unique identifier of the user to disassociate.</param>
    /// <returns>No content on successful disassociation.</returns>
    /// <response code="204">User successfully disassociated from the company.</response>
    /// <response code="404">If the company, user, or association is not found.</response>
    /// <response code="400">If disassociation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /api/companies/550e8400-e29b-41d4-a716-446655440000/users/660e8400-e29b-41d4-a716-446655440000
    ///
    /// Disassociating a user from a company removes their access to company-specific
    /// commercial conditions. The user account itself remains active.
    /// </remarks>
    [HttpDelete("{id:guid}/users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisassociateUser(Guid id, Guid userId)
    {
        try
        {
            _logger.LogInformation("Disassociating user {UserId} from company {CompanyId}", userId, id);

            var result = await _companyService.DisassociateUserAsync(id, userId);

            if (!result)
            {
                _logger.LogWarning("Failed to disassociate user {UserId} from company {CompanyId}", userId, id);
                return NotFound(new { error = $"Association between Company {id} and User {userId} not found." });
            }

            _logger.LogInformation("User {UserId} disassociated successfully from company {CompanyId}", userId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disassociating user {UserId} from company {CompanyId}", userId, id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
