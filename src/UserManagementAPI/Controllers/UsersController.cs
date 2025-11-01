using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.DTOs.Common;
using UserManagementAPI.DTOs.Users;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

/// <summary>
/// Controller for managing user accounts and profiles.
/// </summary>
/// <remarks>
/// This controller provides endpoints for CRUD operations on user accounts,
/// including activation/deactivation and email-based lookups.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    /// <summary>
    /// Initializes a new instance of the UsersController.
    /// </summary>
    /// <param name="userService">The user service for business logic operations.</param>
    /// <param name="logger">The logger instance for diagnostic information.</param>
    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of users.
    /// </summary>
    /// <param name="page">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 10).</param>
    /// <returns>A paginated list of user summaries.</returns>
    /// <response code="200">Returns the paginated list of users.</response>
    /// <response code="400">If the pagination parameters are invalid.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/users?page=1&amp;pageSize=10
    ///
    /// The response includes pagination metadata and user summaries.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResultDTO<UserSummaryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResultDTO<UserSummaryDTO>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Retrieving users - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            if (page < 1 || pageSize < 1)
            {
                return BadRequest(new { error = "Page and pageSize must be greater than 0." });
            }

            var result = await _userService.GetAllAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users list");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves detailed information about a specific user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <returns>Detailed user information including related data.</returns>
    /// <response code="200">Returns the user details.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/users/550e8400-e29b-41d4-a716-446655440000
    ///
    /// Returns comprehensive user information including addresses and company associations.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDetailDTO>> GetUserById(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving user by ID: {UserId}", id);

            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", id);
                return NotFound(new { error = $"User with ID {id} not found." });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user: {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="dto">The user creation data transfer object.</param>
    /// <returns>The newly created user.</returns>
    /// <response code="201">Returns the newly created user.</response>
    /// <response code="400">If the user data is invalid or creation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/users
    ///     {
    ///         "email": "user@example.com",
    ///         "name": "John Doe",
    ///         "cpf": "12345678901",
    ///         "phone": "+5511999999999",
    ///         "userType": "Internal",
    ///         "userRole": "User",
    ///         "addresses": [
    ///             {
    ///                 "street": "Main St",
    ///                 "number": "123",
    ///                 "city": "São Paulo",
    ///                 "state": "SP",
    ///                 "zipCode": "01234567",
    ///                 "country": "Brazil",
    ///                 "addressType": "Billing"
    ///             }
    ///         ]
    ///     }
    ///
    /// Email must be unique in the system.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDTO>> CreateUser([FromBody] CreateUserDTO dto)
    {
        try
        {
            _logger.LogInformation("Creating new user with email: {Email}", dto.Email);

            var user = await _userService.CreateAsync(dto);

            _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing user's information.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="dto">The user update data transfer object.</param>
    /// <returns>The updated user information.</returns>
    /// <response code="200">Returns the updated user.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If the update data is invalid or update fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/users/550e8400-e29b-41d4-a716-446655440000
    ///     {
    ///         "name": "John Updated Doe",
    ///         "phone": "+5511888888888",
    ///         "userRole": "Admin"
    ///     }
    ///
    /// Only provided fields will be updated. Email cannot be changed through this endpoint.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDTO>> UpdateUser(Guid id, [FromBody] UpdateUserDTO dto)
    {
        try
        {
            _logger.LogInformation("Updating user: {UserId}", id);

            var user = await _userService.UpdateAsync(id, dto);

            if (user == null)
            {
                _logger.LogWarning("User not found for update: {UserId}", id);
                return NotFound(new { error = $"User with ID {id} not found." });
            }

            _logger.LogInformation("User updated successfully: {UserId}", id);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a user account.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">User successfully deleted.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If deletion fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /api/users/550e8400-e29b-41d4-a716-446655440000
    ///
    /// This operation performs a soft delete by setting the IsDeleted flag.
    /// User data is retained for audit purposes but the user cannot access the system.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting user: {UserId}", id);

            var result = await _userService.DeleteAsync(id);

            if (!result)
            {
                _logger.LogWarning("User not found for deletion: {UserId}", id);
                return NotFound(new { error = $"User with ID {id} not found." });
            }

            _logger.LogInformation("User deleted successfully: {UserId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Activates a user account.
    /// </summary>
    /// <param name="id">The unique identifier of the user to activate.</param>
    /// <returns>No content on successful activation.</returns>
    /// <response code="204">User successfully activated.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If activation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/users/550e8400-e29b-41d4-a716-446655440000/activate
    ///
    /// Activating a user allows them to access the system and perform operations
    /// according to their assigned role and permissions.
    /// </remarks>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        try
        {
            _logger.LogInformation("Activating user: {UserId}", id);

            var result = await _userService.ActivateAsync(id);

            if (!result)
            {
                _logger.LogWarning("User not found for activation: {UserId}", id);
                return NotFound(new { error = $"User with ID {id} not found." });
            }

            _logger.LogInformation("User activated successfully: {UserId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user: {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deactivates a user account.
    /// </summary>
    /// <param name="id">The unique identifier of the user to deactivate.</param>
    /// <returns>No content on successful deactivation.</returns>
    /// <response code="204">User successfully deactivated.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If deactivation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/users/550e8400-e29b-41d4-a716-446655440000/deactivate
    ///
    /// Deactivating a user prevents them from accessing the system while
    /// retaining their account data. The account can be reactivated later.
    /// </remarks>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        try
        {
            _logger.LogInformation("Deactivating user: {UserId}", id);

            var result = await _userService.DeactivateAsync(id);

            if (!result)
            {
                _logger.LogWarning("User not found for deactivation: {UserId}", id);
                return NotFound(new { error = $"User with ID {id} not found." });
            }

            _logger.LogInformation("User deactivated successfully: {UserId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user: {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <returns>The user information.</returns>
    /// <response code="200">Returns the user details.</response>
    /// <response code="404">If no user is found with the specified email.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/users/email/user@example.com
    ///
    /// Email lookup is case-insensitive and returns the first matching user.
    /// </remarks>
    [HttpGet("email/{email}")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDTO>> GetUserByEmail(string email)
    {
        try
        {
            _logger.LogInformation("Retrieving user by email: {Email}", email);

            var user = await _userService.GetByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("User not found with email: {Email}", email);
                return NotFound(new { error = $"User with email {email} not found." });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            return BadRequest(new { error = ex.Message });
        }
    }
}
