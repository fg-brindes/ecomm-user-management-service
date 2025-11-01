using UserManagementAPI.DTOs.CommercialConditions;
using UserManagementAPI.DTOs.Common;
using UserManagementAPI.Models.Entities;
using UserManagementAPI.Models.Enums;
using UserManagementAPI.Repositories;

namespace UserManagementAPI.Services;

public class CommercialConditionService : ICommercialConditionService
{
    private readonly ICommercialConditionRepository _conditionRepository;
    private readonly IConditionRuleRepository _ruleRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IRepository<CompanyCommercialCondition> _companyConditionRepository;

    public CommercialConditionService(
        ICommercialConditionRepository conditionRepository,
        IConditionRuleRepository ruleRepository,
        ICompanyRepository companyRepository,
        IRepository<CompanyCommercialCondition> companyConditionRepository)
    {
        _conditionRepository = conditionRepository;
        _ruleRepository = ruleRepository;
        _companyRepository = companyRepository;
        _companyConditionRepository = companyConditionRepository;
    }

    public async Task<CommercialConditionDetailDTO?> GetByIdAsync(Guid id)
    {
        var condition = await _conditionRepository.GetByIdWithRulesAsync(id);
        if (condition == null)
            return null;

        return new CommercialConditionDetailDTO
        {
            Id = condition.Id,
            Name = condition.Name,
            Description = condition.Description,
            ValidFrom = condition.ValidFrom,
            ValidUntil = condition.ValidUntil,
            Priority = condition.Priority,
            IsActive = condition.IsActive,
            CreatedAt = condition.CreatedAt,
            UpdatedAt = condition.UpdatedAt,
            Rules = condition.Rules.Select(MapToConditionRuleDTO).ToList()
        };
    }

    public async Task<PaginatedResultDTO<CommercialConditionDTO>> GetAllAsync(int page, int pageSize)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var allConditions = await _conditionRepository.GetAllAsync();
        var conditionsList = allConditions.ToList();

        var totalCount = conditionsList.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var skip = (page - 1) * pageSize;
        var paginatedConditions = conditionsList
            .Skip(skip)
            .Take(pageSize)
            .Select(MapToCommercialConditionDTO)
            .ToList();

        return new PaginatedResultDTO<CommercialConditionDTO>
        {
            Items = paginatedConditions,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        };
    }

    public async Task<CommercialConditionDTO> CreateAsync(CreateCommercialConditionDTO dto)
    {
        // Validate date range if both dates are provided
        if (dto.ValidFrom.HasValue && dto.ValidUntil.HasValue)
        {
            if (dto.ValidFrom.Value > dto.ValidUntil.Value)
                throw new InvalidOperationException("ValidFrom date must be before ValidUntil date.");
        }

        var condition = new CommercialCondition
        {
            Name = dto.Name,
            Description = dto.Description,
            ValidFrom = dto.ValidFrom,
            ValidUntil = dto.ValidUntil,
            Priority = dto.Priority,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdCondition = await _conditionRepository.AddAsync(condition);
        await _conditionRepository.SaveChangesAsync();

        return MapToCommercialConditionDTO(createdCondition);
    }

    public async Task<CommercialConditionDTO?> UpdateAsync(Guid id, UpdateCommercialConditionDTO dto)
    {
        var condition = await _conditionRepository.GetByIdAsync(id);
        if (condition == null)
            return null;

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(dto.Name))
            condition.Name = dto.Name;

        if (dto.Description != null)
            condition.Description = dto.Description;

        if (dto.ValidFrom.HasValue)
            condition.ValidFrom = dto.ValidFrom;

        if (dto.ValidUntil.HasValue)
            condition.ValidUntil = dto.ValidUntil;

        if (dto.Priority.HasValue)
            condition.Priority = dto.Priority.Value;

        // Validate date range if both dates are set
        if (condition.ValidFrom.HasValue && condition.ValidUntil.HasValue)
        {
            if (condition.ValidFrom.Value > condition.ValidUntil.Value)
                throw new InvalidOperationException("ValidFrom date must be before ValidUntil date.");
        }

        condition.UpdatedAt = DateTime.UtcNow;

        await _conditionRepository.UpdateAsync(condition);
        await _conditionRepository.SaveChangesAsync();

        return MapToCommercialConditionDTO(condition);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var condition = await _conditionRepository.GetByIdAsync(id);
        if (condition == null)
            return false;

        await _conditionRepository.DeleteAsync(condition);
        await _conditionRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        var condition = await _conditionRepository.GetByIdAsync(id);
        if (condition == null)
            return false;

        if (condition.IsActive)
            return true; // Already active

        condition.IsActive = true;
        condition.UpdatedAt = DateTime.UtcNow;

        await _conditionRepository.UpdateAsync(condition);
        await _conditionRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var condition = await _conditionRepository.GetByIdAsync(id);
        if (condition == null)
            return false;

        if (!condition.IsActive)
            return true; // Already inactive

        condition.IsActive = false;
        condition.UpdatedAt = DateTime.UtcNow;

        await _conditionRepository.UpdateAsync(condition);
        await _conditionRepository.SaveChangesAsync();

        return true;
    }

    public async Task<ConditionRuleDTO> CreateRuleAsync(Guid conditionId, CreateConditionRuleDTO dto)
    {
        // Validate that the condition exists
        var condition = await _conditionRepository.GetByIdAsync(conditionId);
        if (condition == null)
            throw new InvalidOperationException($"Commercial condition with ID '{conditionId}' not found.");

        // Validate discount rule requirements
        if (dto.RuleType == RuleType.Discount)
        {
            if (!dto.DiscountType.HasValue)
                throw new InvalidOperationException("DiscountType is required for Discount rules.");

            if (!dto.DiscountValue.HasValue || dto.DiscountValue.Value <= 0)
                throw new InvalidOperationException("DiscountValue must be greater than zero for Discount rules.");

            // Validate percentage discount is not greater than 100
            if (dto.DiscountType.Value == DiscountType.Percentage && dto.DiscountValue.Value > 100)
                throw new InvalidOperationException("Percentage discount cannot exceed 100%.");
        }

        var rule = new ConditionRule
        {
            CommercialConditionId = conditionId,
            RuleType = dto.RuleType,
            Expression = dto.Expression,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            Description = dto.Description,
            Priority = dto.Priority,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdRule = await _ruleRepository.AddAsync(rule);
        await _ruleRepository.SaveChangesAsync();

        return MapToConditionRuleDTO(createdRule);
    }

    public async Task<ConditionRuleDTO?> UpdateRuleAsync(Guid conditionId, Guid ruleId, UpdateConditionRuleDTO dto)
    {
        // Validate that the condition exists
        var condition = await _conditionRepository.GetByIdAsync(conditionId);
        if (condition == null)
            return null;

        // Get the rule
        var rule = await _ruleRepository.GetByIdAsync(ruleId);
        if (rule == null || rule.CommercialConditionId != conditionId)
            return null;

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(dto.Expression))
            rule.Expression = dto.Expression;

        if (dto.DiscountType.HasValue)
            rule.DiscountType = dto.DiscountType;

        if (dto.DiscountValue.HasValue)
            rule.DiscountValue = dto.DiscountValue;

        if (dto.Description != null)
            rule.Description = dto.Description;

        if (dto.Priority.HasValue)
            rule.Priority = dto.Priority.Value;

        // Validate discount rule requirements if it's a discount rule
        if (rule.RuleType == RuleType.Discount)
        {
            if (!rule.DiscountType.HasValue)
                throw new InvalidOperationException("DiscountType is required for Discount rules.");

            if (!rule.DiscountValue.HasValue || rule.DiscountValue.Value <= 0)
                throw new InvalidOperationException("DiscountValue must be greater than zero for Discount rules.");

            // Validate percentage discount is not greater than 100
            if (rule.DiscountType.Value == DiscountType.Percentage && rule.DiscountValue.Value > 100)
                throw new InvalidOperationException("Percentage discount cannot exceed 100%.");
        }

        rule.UpdatedAt = DateTime.UtcNow;

        await _ruleRepository.UpdateAsync(rule);
        await _ruleRepository.SaveChangesAsync();

        return MapToConditionRuleDTO(rule);
    }

    public async Task<bool> DeleteRuleAsync(Guid conditionId, Guid ruleId)
    {
        // Validate that the condition exists
        var condition = await _conditionRepository.GetByIdAsync(conditionId);
        if (condition == null)
            return false;

        // Get the rule
        var rule = await _ruleRepository.GetByIdAsync(ruleId);
        if (rule == null || rule.CommercialConditionId != conditionId)
            return false;

        await _ruleRepository.DeleteAsync(rule);
        await _ruleRepository.SaveChangesAsync();

        return true;
    }

    public async Task<List<ConditionRuleDTO>> GetRulesByConditionIdAsync(Guid conditionId)
    {
        // Validate that the condition exists
        var condition = await _conditionRepository.GetByIdAsync(conditionId);
        if (condition == null)
            return new List<ConditionRuleDTO>();

        var rules = await _ruleRepository.GetRulesByConditionIdAsync(conditionId);
        return rules.Select(MapToConditionRuleDTO).ToList();
    }

    public async Task<bool> AssignToCompanyAsync(Guid companyId, Guid conditionId)
    {
        // Validate company exists
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null)
            throw new InvalidOperationException($"Company with ID '{companyId}' not found.");

        // Validate condition exists
        var condition = await _conditionRepository.GetByIdAsync(conditionId);
        if (condition == null)
            throw new InvalidOperationException($"Commercial condition with ID '{conditionId}' not found.");

        // Check if assignment already exists
        var existingAssignments = await _companyConditionRepository.FindAsync(
            cc => cc.CompanyId == companyId && cc.CommercialConditionId == conditionId);

        var existingAssignment = existingAssignments.FirstOrDefault();

        if (existingAssignment != null)
        {
            // If assignment exists but is inactive, reactivate it
            if (!existingAssignment.IsActive)
            {
                existingAssignment.IsActive = true;
                existingAssignment.AssignedAt = DateTime.UtcNow;

                await _companyConditionRepository.UpdateAsync(existingAssignment);
                await _companyConditionRepository.SaveChangesAsync();
                return true;
            }

            // Assignment already active
            return true;
        }

        // Create new assignment
        var companyCondition = new CompanyCommercialCondition
        {
            CompanyId = companyId,
            CommercialConditionId = conditionId,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };

        await _companyConditionRepository.AddAsync(companyCondition);
        await _companyConditionRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UnassignFromCompanyAsync(Guid companyId, Guid conditionId)
    {
        // Validate company exists
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null)
            return false;

        // Find the assignment
        var assignments = await _companyConditionRepository.FindAsync(
            cc => cc.CompanyId == companyId && cc.CommercialConditionId == conditionId && cc.IsActive);

        var assignment = assignments.FirstOrDefault();
        if (assignment == null)
            return false;

        // Deactivate the assignment
        assignment.IsActive = false;

        await _companyConditionRepository.UpdateAsync(assignment);
        await _companyConditionRepository.SaveChangesAsync();

        return true;
    }

    // Manual DTO Mapping Methods
    private static CommercialConditionDTO MapToCommercialConditionDTO(CommercialCondition condition)
    {
        return new CommercialConditionDTO
        {
            Id = condition.Id,
            Name = condition.Name,
            Description = condition.Description,
            ValidFrom = condition.ValidFrom,
            ValidUntil = condition.ValidUntil,
            Priority = condition.Priority,
            IsActive = condition.IsActive,
            CreatedAt = condition.CreatedAt,
            UpdatedAt = condition.UpdatedAt
        };
    }

    private static ConditionRuleDTO MapToConditionRuleDTO(ConditionRule rule)
    {
        return new ConditionRuleDTO
        {
            Id = rule.Id,
            CommercialConditionId = rule.CommercialConditionId,
            RuleType = rule.RuleType,
            Expression = rule.Expression,
            DiscountType = rule.DiscountType,
            DiscountValue = rule.DiscountValue,
            Description = rule.Description,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}
