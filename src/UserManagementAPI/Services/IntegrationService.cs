using UserManagementAPI.DTOs.CommercialConditions;
using UserManagementAPI.DTOs.Integration;
using UserManagementAPI.Models.Entities;
using UserManagementAPI.Models.Enums;
using UserManagementAPI.Repositories;

namespace UserManagementAPI.Services;

public class IntegrationService : IIntegrationService
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICommercialConditionRepository _conditionRepository;
    private readonly IConditionRuleRepository _ruleRepository;

    public IntegrationService(
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        ICommercialConditionRepository conditionRepository,
        IConditionRuleRepository ruleRepository)
    {
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _conditionRepository = conditionRepository;
        _ruleRepository = ruleRepository;
    }

    public async Task<UserCommercialConditionsDTO> GetUserCommercialConditionsAsync(Guid userId)
    {
        // Get user with companies to determine which conditions apply
        var user = await _userRepository.GetByIdWithCompaniesAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User with ID '{userId}' not found.");

        // Get all active conditions for this user
        var conditions = await _conditionRepository.GetConditionsByUserIdAsync(userId);

        // Filter active conditions and order by priority (highest first)
        var activeConditions = conditions
            .Where(c => c.IsActive)
            .Where(c => IsConditionValidByDate(c))
            .OrderByDescending(c => c.Priority)
            .ToList();

        var result = new UserCommercialConditionsDTO
        {
            UserId = userId,
            CompanyId = null,
            Conditions = new List<CommercialConditionWithRulesDTO>()
        };

        // If user has companies, get the primary company
        var activeCompanyAssociation = user.CompanyAssociations
            .Where(ca => ca.IsActive)
            .OrderByDescending(ca => ca.AssociatedAt)
            .FirstOrDefault();

        if (activeCompanyAssociation != null)
        {
            result.CompanyId = activeCompanyAssociation.CompanyId;
        }

        // Map conditions with their rules
        foreach (var condition in activeConditions)
        {
            var conditionWithRules = await _conditionRepository.GetByIdWithRulesAsync(condition.Id);
            if (conditionWithRules != null)
            {
                result.Conditions.Add(MapToCommercialConditionWithRulesDTO(conditionWithRules));
            }
        }

        return result;
    }

    public async Task<UserCommercialConditionsDTO> GetCompanyCommercialConditionsAsync(Guid companyId)
    {
        // Get company with conditions
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null)
            throw new InvalidOperationException($"Company with ID '{companyId}' not found.");

        // Get all active conditions for this company
        var conditions = await _conditionRepository.GetConditionsByCompanyIdAsync(companyId);

        // Filter active conditions and order by priority (highest first)
        var activeConditions = conditions
            .Where(c => c.IsActive)
            .Where(c => IsConditionValidByDate(c))
            .OrderByDescending(c => c.Priority)
            .ToList();

        var result = new UserCommercialConditionsDTO
        {
            UserId = Guid.Empty, // No specific user
            CompanyId = companyId,
            Conditions = new List<CommercialConditionWithRulesDTO>()
        };

        // Map conditions with their rules
        foreach (var condition in activeConditions)
        {
            var conditionWithRules = await _conditionRepository.GetByIdWithRulesAsync(condition.Id);
            if (conditionWithRules != null)
            {
                result.Conditions.Add(MapToCommercialConditionWithRulesDTO(conditionWithRules));
            }
        }

        return result;
    }

    public async Task<VisibilityRulesDTO> GetVisibilityRulesAsync(Guid userId, Guid? companyId)
    {
        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User with ID '{userId}' not found.");

        // Get visibility rules for the user and optionally company
        var rules = await _ruleRepository.GetVisibilityRulesByUserIdAsync(userId, companyId);

        // Filter active rules and get their parent conditions to check validity
        var validRules = new List<ConditionRule>();
        foreach (var rule in rules.Where(r => r.IsActive))
        {
            var condition = await _conditionRepository.GetByIdAsync(rule.CommercialConditionId);
            if (condition != null && condition.IsActive && IsConditionValidByDate(condition))
            {
                validRules.Add(rule);
            }
        }

        // Order by priority (highest first)
        var orderedRules = validRules.OrderByDescending(r => r.Priority).ToList();

        var result = new VisibilityRulesDTO
        {
            UserId = userId,
            CompanyId = companyId,
            Rules = new List<VisibilityRuleDTO>()
        };

        // Map to visibility rule DTOs
        foreach (var rule in orderedRules)
        {
            var condition = await _conditionRepository.GetByIdAsync(rule.CommercialConditionId);
            if (condition != null)
            {
                result.Rules.Add(new VisibilityRuleDTO
                {
                    RuleId = rule.Id,
                    ConditionName = condition.Name,
                    Expression = rule.Expression,
                    Priority = rule.Priority
                });
            }
        }

        return result;
    }

    public async Task<DiscountRulesDTO> GetDiscountRulesAsync(Guid userId, Guid? companyId)
    {
        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User with ID '{userId}' not found.");

        // Get discount rules for the user and optionally company
        var rules = await _ruleRepository.GetDiscountRulesByUserIdAsync(userId, companyId);

        // Filter active rules and get their parent conditions to check validity
        var validRules = new List<ConditionRule>();
        foreach (var rule in rules.Where(r => r.IsActive))
        {
            var condition = await _conditionRepository.GetByIdAsync(rule.CommercialConditionId);
            if (condition != null && condition.IsActive && IsConditionValidByDate(condition))
            {
                validRules.Add(rule);
            }
        }

        // Order by priority (highest first)
        var orderedRules = validRules.OrderByDescending(r => r.Priority).ToList();

        var result = new DiscountRulesDTO
        {
            UserId = userId,
            CompanyId = companyId,
            Rules = new List<DiscountRuleDTO>()
        };

        // Map to discount rule DTOs
        foreach (var rule in orderedRules)
        {
            var condition = await _conditionRepository.GetByIdAsync(rule.CommercialConditionId);
            if (condition != null && rule.DiscountType.HasValue && rule.DiscountValue.HasValue)
            {
                result.Rules.Add(new DiscountRuleDTO
                {
                    RuleId = rule.Id,
                    ConditionName = condition.Name,
                    Expression = rule.Expression,
                    DiscountType = rule.DiscountType.Value,
                    DiscountValue = rule.DiscountValue.Value,
                    Priority = rule.Priority
                });
            }
        }

        return result;
    }

    public async Task<UserExpressionContextDTO?> GetUserExpressionContextAsync(Guid userId)
    {
        // Get user with companies
        var user = await _userRepository.GetByIdWithCompaniesAsync(userId);
        if (user == null)
            return null;

        var result = new UserExpressionContextDTO
        {
            UserId = user.Id,
            UserType = user.UserType,
            Company = null
        };

        // Get the primary company if user has company associations
        var activeCompanyAssociation = user.CompanyAssociations
            .Where(ca => ca.IsActive)
            .OrderByDescending(ca => ca.AssociatedAt)
            .FirstOrDefault();

        if (activeCompanyAssociation != null)
        {
            var company = await _companyRepository.GetByIdAsync(activeCompanyAssociation.CompanyId);
            if (company != null && company.IsActive)
            {
                result.Company = new CompanyContextDTO
                {
                    CompanyId = company.Id,
                    Cnpj = company.Cnpj,
                    TradeName = company.TradeName ?? company.CorporateName
                };
            }
        }

        return result;
    }

    public async Task<AccessCheckDTO?> GetAccessCheckAsync(Guid userId)
    {
        // Get user with companies
        var user = await _userRepository.GetByIdWithCompaniesAsync(userId);
        if (user == null)
            return null;

        var result = new AccessCheckDTO
        {
            UserId = user.Id,
            IsActive = user.IsActive,
            HasCompany = false,
            CompanyIsActive = false,
            HasActiveConditions = false
        };

        // Check if user has active company associations
        var activeCompanyAssociation = user.CompanyAssociations
            .Where(ca => ca.IsActive)
            .FirstOrDefault();

        if (activeCompanyAssociation != null)
        {
            result.HasCompany = true;

            // Check if the company is active
            var company = await _companyRepository.GetByIdAsync(activeCompanyAssociation.CompanyId);
            if (company != null && company.IsActive)
            {
                result.CompanyIsActive = true;

                // Check if the company has active commercial conditions
                var conditions = await _conditionRepository.GetConditionsByCompanyIdAsync(company.Id);
                var hasActiveConditions = conditions
                    .Any(c => c.IsActive && IsConditionValidByDate(c));

                result.HasActiveConditions = hasActiveConditions;
            }
        }
        else
        {
            // User without company - check if user has direct conditions
            var conditions = await _conditionRepository.GetConditionsByUserIdAsync(userId);
            var hasActiveConditions = conditions
                .Any(c => c.IsActive && IsConditionValidByDate(c));

            result.HasActiveConditions = hasActiveConditions;
        }

        return result;
    }

    // Helper Methods
    private static bool IsConditionValidByDate(CommercialCondition condition)
    {
        var now = DateTime.UtcNow;

        // If ValidFrom is set, check if current date is after or equal
        if (condition.ValidFrom.HasValue && now < condition.ValidFrom.Value)
            return false;

        // If ValidUntil is set, check if current date is before or equal
        if (condition.ValidUntil.HasValue && now > condition.ValidUntil.Value)
            return false;

        return true;
    }

    private static CommercialConditionWithRulesDTO MapToCommercialConditionWithRulesDTO(CommercialCondition condition)
    {
        return new CommercialConditionWithRulesDTO
        {
            ConditionId = condition.Id,
            Name = condition.Name,
            ValidFrom = condition.ValidFrom,
            ValidUntil = condition.ValidUntil,
            Priority = condition.Priority,
            Rules = condition.Rules
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.Priority)
                .Select(MapToConditionRuleDTO)
                .ToList()
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
