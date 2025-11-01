using Microsoft.AspNetCore.Mvc;

namespace UserManagementAPI.Controllers;

/// <summary>
/// Controller for health check endpoints.
/// </summary>
/// <remarks>
/// This controller provides simple health check endpoints for monitoring
/// service availability and status. Used by container orchestrators,
/// load balancers, and monitoring systems.
/// </remarks>
[ApiController]
[Produces("application/json")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Initializes a new instance of the HealthController.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic information.</param>
    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Performs a basic health check of the service.
    /// </summary>
    /// <returns>Health status and timestamp.</returns>
    /// <response code="200">Service is healthy and operational.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /health
    ///
    /// This endpoint returns a simple OK status with the current server timestamp.
    /// It indicates that the service is running and can process HTTP requests.
    ///
    /// Sample response:
    ///
    ///     {
    ///         "status": "Healthy",
    ///         "timestamp": "2025-11-01T10:30:00Z",
    ///         "service": "User Management API"
    ///     }
    ///
    /// This is a lightweight endpoint designed for frequent polling by health
    /// monitoring systems without impacting service performance.
    /// </remarks>
    [HttpGet("/health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        _logger.LogDebug("Health check endpoint called");

        var healthStatus = new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            service = "User Management API"
        };

        return Ok(healthStatus);
    }
}
