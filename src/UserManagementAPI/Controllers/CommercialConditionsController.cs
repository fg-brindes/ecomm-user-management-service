using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.DTOs.CommercialConditions;
using UserManagementAPI.DTOs.Common;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

/// <summary>
/// Controller for managing commercial conditions and their associated rules.
/// </summary>
/// <remarks>
/// This controller provides endpoints for CRUD operations on commercial conditions,
/// including rule management, activation/deactivation, and company assignments.
/// Commercial conditions define visibility and discount rules that can be applied
/// to products and transactions based on user or company context.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Commercial Conditions")]
public class CommercialConditionsController : ControllerBase
{
    private readonly ICommercialConditionService _conditionService;
    private readonly ILogger<CommercialConditionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the CommercialConditionsController.
    /// </summary>
    /// <param name="conditionService">The commercial condition service for business logic operations.</param>
    /// <param name="logger">The logger instance for diagnostic information.</param>
    public CommercialConditionsController(
        ICommercialConditionService conditionService,
        ILogger<CommercialConditionsController> logger)
    {
        _conditionService = conditionService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of commercial conditions.
    /// </summary>
    /// <param name="page">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 10).</param>
    /// <returns>A paginated list of commercial conditions.</returns>
    /// <response code="200">Returns the paginated list of commercial conditions.</response>
    /// <response code="400">If the pagination parameters are invalid.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/commercialconditions?page=1&amp;pageSize=10
    ///
    /// The response includes pagination metadata and commercial condition summaries.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResultDTO<CommercialConditionDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResultDTO<CommercialConditionDTO>>> GetCommercialConditions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Retrieving commercial conditions - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            if (page < 1 || pageSize < 1)
            {
                return BadRequest(new { error = "Page and pageSize must be greater than 0." });
            }

            var result = await _conditionService.GetAllAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving commercial conditions list");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves detailed information about a specific commercial condition.
    /// </summary>
    /// <param name="id">The unique identifier of the commercial condition.</param>
    /// <returns>Detailed commercial condition information including all associated rules.</returns>
    /// <response code="200">Returns the commercial condition details.</response>
    /// <response code="404">If the commercial condition is not found.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/commercialconditions/550e8400-e29b-41d4-a716-446655440000
    ///
    /// Returns comprehensive commercial condition information including all visibility
    /// and discount rules associated with this condition.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CommercialConditionDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommercialConditionDetailDTO>> GetCommercialConditionById(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving commercial condition by ID: {ConditionId}", id);

            var condition = await _conditionService.GetByIdAsync(id);

            if (condition == null)
            {
                _logger.LogWarning("Commercial condition not found: {ConditionId}", id);
                return NotFound(new { error = $"Commercial condition with ID {id} not found." });
            }

            return Ok(condition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving commercial condition: {ConditionId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new commercial condition.
    /// </summary>
    /// <param name="dto">The commercial condition creation data transfer object.</param>
    /// <returns>The newly created commercial condition.</returns>
    /// <response code="201">Returns the newly created commercial condition.</response>
    /// <response code="400">If the commercial condition data is invalid or creation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/commercialconditions
    ///     {
    ///         "name": "Premium Customer Condition",
    ///         "description": "Special pricing and visibility for premium customers",
    ///         "priority": 10
    ///     }
    ///
    /// Priority determines the order of condition evaluation. Higher priority conditions
    /// are evaluated first. After creating a condition, add rules to define specific
    /// visibility and discount behaviors.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(CommercialConditionDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommercialConditionDTO>> CreateCommercialCondition(
        [FromBody] CreateCommercialConditionDTO dto)
    {
        try
        {
            _logger.LogInformation("Creating new commercial condition: {Name}", dto.Name);

            var condition = await _conditionService.CreateAsync(dto);

            _logger.LogInformation("Commercial condition created successfully with ID: {ConditionId}", condition.Id);
            return CreatedAtAction(nameof(GetCommercialConditionById), new { id = condition.Id }, condition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating commercial condition");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing commercial condition's information.
    /// </summary>
    /// <param name="id">The unique identifier of the commercial condition to update.</param>
    /// <param name="dto">The commercial condition update data transfer object.</param>
    /// <returns>The updated commercial condition information.</returns>
    /// <response code="200">Returns the updated commercial condition.</response>
    /// <response code="404">If the commercial condition is not found.</response>
    /// <response code="400">If the update data is invalid or update fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/commercialconditions/550e8400-e29b-41d4-a716-446655440000
    ///     {
    ///         "name": "Updated Premium Condition",
    ///         "description": "Updated description",
    ///         "priority": 15
    ///     }
    ///
    /// Only provided fields will be updated. To modify rules, use the dedicated rule
    /// management endpoints.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CommercialConditionDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommercialConditionDTO>> UpdateCommercialCondition(
        Guid id,
        [FromBody] UpdateCommercialConditionDTO dto)
    {
        try
        {
            _logger.LogInformation("Updating commercial condition: {ConditionId}", id);

            var condition = await _conditionService.UpdateAsync(id, dto);

            if (condition == null)
            {
                _logger.LogWarning("Commercial condition not found for update: {ConditionId}", id);
                return NotFound(new { error = $"Commercial condition with ID {id} not found." });
            }

            _logger.LogInformation("Commercial condition updated successfully: {ConditionId}", id);
            return Ok(condition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating commercial condition: {ConditionId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a commercial condition.
    /// </summary>
    /// <param name="id">The unique identifier of the commercial condition to delete.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">Commercial condition successfully deleted.</response>
    /// <response code="404">If the commercial condition is not found.</response>
    /// <response code="400">If deletion fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /api/commercialconditions/550e8400-e29b-41d4-a716-446655440000
    ///
    /// This operation performs a soft delete by setting the IsDeleted flag.
    /// All associated rules and company assignments are also soft deleted.
    /// Data is retained for audit purposes.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCommercialCondition(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting commercial condition: {ConditionId}", id);

            var result = await _conditionService.DeleteAsync(id);

            if (!result)
            {
                _logger.LogWarning("Commercial condition not found for deletion: {ConditionId}", id);
                return NotFound(new { error = $"Commercial condition with ID {id} not found." });
            }

            _logger.LogInformation("Commercial condition deleted successfully: {ConditionId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting commercial condition: {ConditionId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Activates a commercial condition.
    /// </summary>
    /// <param name="id">The unique identifier of the commercial condition to activate.</param>
    /// <returns>No content on successful activation.</returns>
    /// <response code="204">Commercial condition successfully activated.</response>
    /// <response code="404">If the commercial condition is not found.</response>
    /// <response code="400">If activation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/commercialconditions/550e8400-e29b-41d4-a716-446655440000/activate
    ///
    /// Activating a commercial condition enables its rules to be applied during
    /// product visibility checks and discount calculations.
    /// </remarks>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateCommercialCondition(Guid id)
    {
        try
        {
            _logger.LogInformation("Activating commercial condition: {ConditionId}", id);

            var result = await _conditionService.ActivateAsync(id);

            if (!result)
            {
                _logger.LogWarning("Commercial condition not found for activation: {ConditionId}", id);
                return NotFound(new { error = $"Commercial condition with ID {id} not found." });
            }

            _logger.LogInformation("Commercial condition activated successfully: {ConditionId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating commercial condition: {ConditionId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deactivates a commercial condition.
    /// </summary>
    /// <param name="id">The unique identifier of the commercial condition to deactivate.</param>
    /// <returns>No content on successful deactivation.</returns>
    /// <response code="204">Commercial condition successfully deactivated.</response>
    /// <response code="404">If the commercial condition is not found.</response>
    /// <response code="400">If deactivation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/commercialconditions/550e8400-e29b-41d4-a716-446655440000/deactivate
    ///
    /// Deactivating a commercial condition prevents its rules from being applied
    /// while retaining the configuration. The condition can be reactivated later.
    /// </remarks>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateCommercialCondition(Guid id)
    {
        try
        {
            _logger.LogInformation("Deactivating commercial condition: {ConditionId}", id);

            var result = await _conditionService.DeactivateAsync(id);

            if (!result)
            {
                _logger.LogWarning("Commercial condition not found for deactivation: {ConditionId}", id);
                return NotFound(new { error = $"Commercial condition with ID {id} not found." });
            }

            _logger.LogInformation("Commercial condition deactivated successfully: {ConditionId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating commercial condition: {ConditionId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all rules associated with a commercial condition.
    /// </summary>
    /// <param name="id">The unique identifier of the commercial condition.</param>
    /// <returns>A list of all rules for the specified commercial condition.</returns>
    /// <response code="200">Returns the list of rules.</response>
    /// <response code="404">If the commercial condition is not found.</response>
    /// <response code="400">If an error occurs during retrieval.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/commercialconditions/550e8400-e29b-41d4-a716-446655440000/rules
    ///
    /// Returns all visibility and discount rules configured for this commercial condition.
    /// Rules define the specific behaviors and criteria for applying the condition.
    /// </remarks>
    [HttpGet("{id:guid}/rules")]
    [ProducesResponseType(typeof(List<ConditionRuleDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ConditionRuleDTO>>> GetRulesByConditionId(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving rules for commercial condition: {ConditionId}", id);

            var rules = await _conditionService.GetRulesByConditionIdAsync(id);

            if (rules == null)
            {
                _logger.LogWarning("Commercial condition not found: {ConditionId}", id);
                return NotFound(new { error = $"Commercial condition with ID {id} not found." });
            }

            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rules for commercial condition: {ConditionId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new rule for a commercial condition.
    /// </summary>
    /// <param name="id">The unique identifier of the commercial condition.</param>
    /// <param name="dto">The rule creation data transfer object.</param>
    /// <returns>The newly created rule.</returns>
    /// <response code="201">Returns the newly created rule.</response>
    /// <response code="404">If the commercial condition is not found.</response>
    /// <response code="400">If the rule data is invalid or creation fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/commercialconditions/550e8400-e29b-41d4-a716-446655440000/rules
    ///     {
    ///         "ruleType": "Visibility",
    ///         "expression": "product.category == 'Premium'",
    ///         "priority": 10,
    ///         "discountType": null,
    ///         "discountValue": null
    ///     }
    ///
    /// For visibility rules, expression evaluates to true/false for product visibility.
    /// For discount rules, specify discountType (Percentage/Fixed) and discountValue.
    /// </remarks>
    [HttpPost("{id:guid}/rules")]
    [ProducesResponseType(typeof(ConditionRuleDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConditionRuleDTO>> CreateRule(Guid id, [FromBody] CreateConditionRuleDTO dto)
    {
        try
        {
            _logger.LogInformation("Creating rule for commercial condition: {ConditionId}", id);

            var rule = await _conditionService.CreateRuleAsync(id, dto);

            _logger.LogInformation("Rule created successfully for commercial condition: {ConditionId}", id);
            return CreatedAtAction(
                nameof(GetRulesByConditionId),
                new { id = id },
                rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rule for commercial condition: {ConditionId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing rule within a commercial condition.
    /// </summary>
    /// <param name="id">The unique identifier of the commercial condition.</param>
    /// <param name="ruleId">The unique identifier of the rule to update.</param>
    /// <param name="dto">The rule update data transfer object.</param>
    /// <returns>The updated rule information.</returns>
    /// <response code="200">Returns the updated rule.</response>
    /// <response code="404">If the commercial condition or rule is not found.</response>
    /// <response code="400">If the update data is invalid or update fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/commercialconditions/550e8400-e29b-41d4-a716-446655440000/rules/660e8400-e29b-41d4-a716-446655440000
    ///     {
    ///         "expression": "product.category == 'Premium' &amp;&amp; product.price > 1000",
    ///         "priority": 15
    ///     }
    ///
    /// Only provided fields will be updated. RuleType cannot be changed after creation.
    /// </remarks>
    [HttpPut("{id:guid}/rules/{ruleId:guid}")]
    [ProducesResponseType(typeof(ConditionRuleDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConditionRuleDTO>> UpdateRule(
        Guid id,
        Guid ruleId,
        [FromBody] UpdateConditionRuleDTO dto)
    {
        try
        {
            _logger.LogInformation("Updating rule {RuleId} for commercial condition: {ConditionId}", ruleId, id);

            var rule = await _conditionService.UpdateRuleAsync(id, ruleId, dto);

            if (rule == null)
            {
                _logger.LogWarning("Rule {RuleId} or commercial condition {ConditionId} not found", ruleId, id);
                return NotFound(new { error = $"Rule {ruleId} not found in commercial condition {id}." });
            }

            _logger.LogInformation("Rule {RuleId} updated successfully", ruleId);
            return Ok(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule {RuleId} for commercial condition: {ConditionId}", ruleId, id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a rule from a commercial condition.
    /// </summary>
    /// <param name="id">The unique identifier of the commercial condition.</param>
    /// <param name="ruleId">The unique identifier of the rule to delete.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">Rule successfully deleted.</response>
    /// <response code="404">If the commercial condition or rule is not found.</response>
    /// <response code="400">If deletion fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /api/commercialconditions/550e8400-e29b-41d4-a716-446655440000/rules/660e8400-e29b-41d4-a716-446655440000
    ///
    /// This operation performs a soft delete by setting the IsDeleted flag.
    /// The rule configuration is retained for audit purposes.
    /// </remarks>
    [HttpDelete("{id:guid}/rules/{ruleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRule(Guid id, Guid ruleId)
    {
        try
        {
            _logger.LogInformation("Deleting rule {RuleId} from commercial condition: {ConditionId}", ruleId, id);

            var result = await _conditionService.DeleteRuleAsync(id, ruleId);

            if (!result)
            {
                _logger.LogWarning("Rule {RuleId} or commercial condition {ConditionId} not found", ruleId, id);
                return NotFound(new { error = $"Rule {ruleId} not found in commercial condition {id}." });
            }

            _logger.LogInformation("Rule {RuleId} deleted successfully", ruleId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rule {RuleId} from commercial condition: {ConditionId}", ruleId, id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Assigns a commercial condition to a company.
    /// </summary>
    /// <param name="companyId">The unique identifier of the company.</param>
    /// <param name="conditionId">The unique identifier of the commercial condition to assign.</param>
    /// <returns>No content on successful assignment.</returns>
    /// <response code="204">Commercial condition successfully assigned to the company.</response>
    /// <response code="404">If the company or commercial condition is not found.</response>
    /// <response code="400">If the assignment fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/commercialconditions/companies/550e8400-e29b-41d4-a716-446655440000/conditions/660e8400-e29b-41d4-a716-446655440000
    ///
    /// Assigning a commercial condition to a company makes the condition's rules
    /// applicable to all users associated with that company. Multiple conditions
    /// can be assigned to the same company, evaluated by priority.
    /// </remarks>
    [HttpPost("companies/{companyId:guid}/conditions/{conditionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignToCompany(Guid companyId, Guid conditionId)
    {
        try
        {
            _logger.LogInformation(
                "Assigning commercial condition {ConditionId} to company {CompanyId}",
                conditionId,
                companyId);

            var result = await _conditionService.AssignToCompanyAsync(companyId, conditionId);

            if (!result)
            {
                _logger.LogWarning(
                    "Failed to assign commercial condition {ConditionId} to company {CompanyId}",
                    conditionId,
                    companyId);
                return NotFound(new { error = $"Company {companyId} or Commercial Condition {conditionId} not found." });
            }

            _logger.LogInformation(
                "Commercial condition {ConditionId} assigned successfully to company {CompanyId}",
                conditionId,
                companyId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error assigning commercial condition {ConditionId} to company {CompanyId}",
                conditionId,
                companyId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Unassigns a commercial condition from a company.
    /// </summary>
    /// <param name="companyId">The unique identifier of the company.</param>
    /// <param name="conditionId">The unique identifier of the commercial condition to unassign.</param>
    /// <returns>No content on successful unassignment.</returns>
    /// <response code="204">Commercial condition successfully unassigned from the company.</response>
    /// <response code="404">If the company, commercial condition, or assignment is not found.</response>
    /// <response code="400">If the unassignment fails.</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /api/commercialconditions/companies/550e8400-e29b-41d4-a716-446655440000/conditions/660e8400-e29b-41d4-a716-446655440000
    ///
    /// Unassigning a commercial condition from a company prevents the condition's
    /// rules from being applied to that company's users. The condition itself
    /// remains available for assignment to other companies.
    /// </remarks>
    [HttpDelete("companies/{companyId:guid}/conditions/{conditionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnassignFromCompany(Guid companyId, Guid conditionId)
    {
        try
        {
            _logger.LogInformation(
                "Unassigning commercial condition {ConditionId} from company {CompanyId}",
                conditionId,
                companyId);

            var result = await _conditionService.UnassignFromCompanyAsync(companyId, conditionId);

            if (!result)
            {
                _logger.LogWarning(
                    "Failed to unassign commercial condition {ConditionId} from company {CompanyId}",
                    conditionId,
                    companyId);
                return NotFound(new { error = $"Assignment between Company {companyId} and Commercial Condition {conditionId} not found." });
            }

            _logger.LogInformation(
                "Commercial condition {ConditionId} unassigned successfully from company {CompanyId}",
                conditionId,
                companyId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error unassigning commercial condition {ConditionId} from company {CompanyId}",
                conditionId,
                companyId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
