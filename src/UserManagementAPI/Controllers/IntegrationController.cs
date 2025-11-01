using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.DTOs.Integration;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

/// <summary>
/// Controller for integration endpoints consumed by other microservices.
/// </summary>
/// <remarks>
/// This controller provides specialized endpoints designed for service-to-service
/// communication. These endpoints aggregate data from multiple sources to support
/// other services in the e-commerce platform, such as the Product Catalog,
/// Cart, and Quote services.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Integration")]
public class IntegrationController : ControllerBase
{
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<IntegrationController> _logger;

    /// <summary>
    /// Initializes a new instance of the IntegrationController.
    /// </summary>
    /// <param name="integrationService">The integration service for business logic operations.</param>
    /// <param name="logger">The logger instance for diagnostic information.</param>
    public IntegrationController(
        IIntegrationService integrationService,
        ILogger<IntegrationController> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all commercial conditions associated with a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>User's commercial conditions including all associated rules.</returns>
    /// <response code="200">Returns the user's commercial conditions.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/integration/users/550e8400-e29b-41d4-a716-446655440000/commercial-conditions
    ///
    /// This endpoint aggregates commercial conditions from:
    /// - Direct user assignments
    /// - All companies the user is associated with
    ///
    /// Used by Product Catalog service to determine product visibility and pricing
    /// for a specific user context.
    /// </remarks>
    [HttpGet("users/{userId:guid}/commercial-conditions")]
    [ProducesResponseType(typeof(UserCommercialConditionsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserCommercialConditionsDTO>> GetUserCommercialConditions(Guid userId)
    {
        try
        {
            _logger.LogInformation("Retrieving commercial conditions for user: {UserId}", userId);

            var conditions = await _integrationService.GetUserCommercialConditionsAsync(userId);

            if (conditions == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return NotFound(new { error = $"User with ID {userId} not found." });
            }

            return Ok(conditions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving commercial conditions for user: {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all commercial conditions associated with a specific company.
    /// </summary>
    /// <param name="companyId">The unique identifier of the company.</param>
    /// <returns>Company's commercial conditions including all associated rules.</returns>
    /// <response code="200">Returns the company's commercial conditions.</response>
    /// <response code="404">If the company is not found.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/integration/companies/550e8400-e29b-41d4-a716-446655440000/commercial-conditions
    ///
    /// This endpoint retrieves all commercial conditions assigned to a company.
    ///
    /// Used by other services to understand what conditions apply when users
    /// are operating in a company context.
    /// </remarks>
    [HttpGet("companies/{companyId:guid}/commercial-conditions")]
    [ProducesResponseType(typeof(UserCommercialConditionsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserCommercialConditionsDTO>> GetCompanyCommercialConditions(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Retrieving commercial conditions for company: {CompanyId}", companyId);

            var conditions = await _integrationService.GetCompanyCommercialConditionsAsync(companyId);

            if (conditions == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", companyId);
                return NotFound(new { error = $"Company with ID {companyId} not found." });
            }

            return Ok(conditions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving commercial conditions for company: {CompanyId}", companyId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves visibility rules applicable to a user context.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="companyId">Optional company identifier to filter rules by company context.</param>
    /// <returns>Visibility rules with expressions and criteria.</returns>
    /// <response code="200">Returns the applicable visibility rules.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/integration/visibility-rules?userId=550e8400-e29b-41d4-a716-446655440000&amp;companyId=660e8400-e29b-41d4-a716-446655440000
    ///
    /// Visibility rules determine which products should be displayed to a user.
    /// If companyId is provided, only rules applicable to that company context are returned.
    ///
    /// Used by Product Catalog service to filter product lists based on user permissions.
    /// </remarks>
    [HttpGet("visibility-rules")]
    [ProducesResponseType(typeof(VisibilityRulesDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VisibilityRulesDTO>> GetVisibilityRules(
        [FromQuery] Guid userId,
        [FromQuery] Guid? companyId = null)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving visibility rules for user: {UserId}, company: {CompanyId}",
                userId,
                companyId);

            var rules = await _integrationService.GetVisibilityRulesAsync(userId, companyId);

            if (rules == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return NotFound(new { error = $"User with ID {userId} not found." });
            }

            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving visibility rules for user: {UserId}, company: {CompanyId}",
                userId,
                companyId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves discount rules applicable to a user context.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="companyId">Optional company identifier to filter rules by company context.</param>
    /// <returns>Discount rules with expressions, types, and values.</returns>
    /// <response code="200">Returns the applicable discount rules.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/integration/discount-rules?userId=550e8400-e29b-41d4-a716-446655440000&amp;companyId=660e8400-e29b-41d4-a716-446655440000
    ///
    /// Discount rules define pricing adjustments based on user and company context.
    /// Rules include the discount type (Percentage/Fixed), value, and conditions.
    /// If companyId is provided, only rules applicable to that company context are returned.
    ///
    /// Used by Cart and Quote services to calculate final pricing.
    /// </remarks>
    [HttpGet("discount-rules")]
    [ProducesResponseType(typeof(DiscountRulesDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DiscountRulesDTO>> GetDiscountRules(
        [FromQuery] Guid userId,
        [FromQuery] Guid? companyId = null)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving discount rules for user: {UserId}, company: {CompanyId}",
                userId,
                companyId);

            var rules = await _integrationService.GetDiscountRulesAsync(userId, companyId);

            if (rules == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return NotFound(new { error = $"User with ID {userId} not found." });
            }

            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving discount rules for user: {UserId}, company: {CompanyId}",
                userId,
                companyId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves the expression evaluation context for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>User context data for expression evaluation.</returns>
    /// <response code="200">Returns the user expression context.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/integration/users/550e8400-e29b-41d4-a716-446655440000/expression-context
    ///
    /// The expression context provides all user-related data needed to evaluate
    /// rule expressions, including:
    /// - User properties (type, role, etc.)
    /// - Associated companies
    /// - User-specific attributes
    ///
    /// Used by other services to evaluate complex business rules and expressions
    /// that reference user context.
    /// </remarks>
    [HttpGet("users/{userId:guid}/expression-context")]
    [ProducesResponseType(typeof(UserExpressionContextDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserExpressionContextDTO>> GetUserExpressionContext(Guid userId)
    {
        try
        {
            _logger.LogInformation("Retrieving expression context for user: {UserId}", userId);

            var context = await _integrationService.GetUserExpressionContextAsync(userId);

            if (context == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return NotFound(new { error = $"User with ID {userId} not found." });
            }

            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expression context for user: {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Performs an access check for a user to determine their system access status.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>Access check result with status and details.</returns>
    /// <response code="200">Returns the access check result.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If an error occurs during the check.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/integration/users/550e8400-e29b-41d4-a716-446655440000/access-check
    ///
    /// The access check verifies:
    /// - User exists and is not deleted
    /// - User is active (not deactivated)
    /// - User has appropriate permissions
    /// - Any additional access requirements
    ///
    /// Used by authentication/authorization services and API gateways to validate
    /// user access before processing requests.
    /// </remarks>
    [HttpGet("users/{userId:guid}/access-check")]
    [ProducesResponseType(typeof(AccessCheckDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccessCheckDTO>> GetAccessCheck(Guid userId)
    {
        try
        {
            _logger.LogInformation("Performing access check for user: {UserId}", userId);

            var accessCheck = await _integrationService.GetAccessCheckAsync(userId);

            if (accessCheck == null)
            {
                _logger.LogWarning("User not found for access check: {UserId}", userId);
                return NotFound(new { error = $"User with ID {userId} not found." });
            }

            return Ok(accessCheck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing access check for user: {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
