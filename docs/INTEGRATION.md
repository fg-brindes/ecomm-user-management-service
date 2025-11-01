# Integration Guide

This guide explains how to integrate other microservices with the User Management Service, with detailed examples for common integration scenarios.

## Table of Contents

- [Overview](#overview)
- [Integration Architecture](#integration-architecture)
- [Authentication](#authentication)
- [Integration Endpoints](#integration-endpoints)
- [Catalog Service Integration](#catalog-service-integration)
- [Cart & Quote Service Integration](#cart--quote-service-integration)
- [Expression Context](#expression-context)
- [Code Examples](#code-examples)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

The User Management Service provides specialized integration endpoints designed for service-to-service communication. These endpoints aggregate data from multiple sources to support business logic in other microservices.

### Key Integration Points

1. **Catalog Service**: Product visibility and filtering
2. **Cart/Quote Service**: Discount calculations and pricing
3. **Authentication Service**: User access control
4. **Order Service**: User and company information retrieval

## Integration Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    E-Commerce Platform                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────┐         ┌──────────────────────┐             │
│  │   Frontend   │────────▶│  API Gateway/Auth    │             │
│  │  (React/etc) │         │      Service         │             │
│  └──────────────┘         └──────────┬───────────┘             │
│                                       │                          │
│                           ┌───────────▼──────────┐              │
│                           │  User Management     │              │
│                           │      Service         │              │
│                           │  (This Service)      │              │
│                           └───────────┬──────────┘              │
│                                       │                          │
│         ┌─────────────────────────────┼─────────────────┐       │
│         │                             │                 │       │
│  ┌──────▼────────┐          ┌─────────▼──────┐  ┌──────▼─────┐│
│  │   Catalog     │          │   Cart/Quote   │  │   Order    ││
│  │   Service     │          │    Service     │  │  Service   ││
│  │               │          │                │  │            ││
│  │ - Visibility  │          │ - Discounts    │  │ - User     ││
│  │   Rules       │          │ - Pricing      │  │   Data     ││
│  └───────────────┘          └────────────────┘  └────────────┘│
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Communication Flow

1. **Request Authentication**: Frontend → API Gateway
2. **User Context Retrieval**: API Gateway → User Management Service
3. **Business Logic Execution**: Catalog/Cart Services → User Management Service
4. **Response Assembly**: Services aggregate data and return to Frontend

## Authentication

### Current Implementation

**Note**: The User Management Service currently does not implement authentication. All endpoints are accessible without tokens. This is suitable for internal service-to-service communication within a trusted network.

### Future Implementation

When authentication is added, integration requests will require JWT tokens:

```http
Authorization: Bearer <jwt-token>
```

Service accounts will be created for inter-service communication.

## Integration Endpoints

All integration endpoints are prefixed with `/api/integration`.

### Available Endpoints

| Endpoint | Purpose | Primary Consumer |
|----------|---------|------------------|
| `GET /users/{userId}/commercial-conditions` | Get all conditions for a user | Catalog, Cart |
| `GET /companies/{companyId}/commercial-conditions` | Get all conditions for a company | Catalog, Cart |
| `GET /visibility-rules` | Get visibility rules for product filtering | Catalog |
| `GET /discount-rules` | Get discount rules for pricing | Cart, Quote |
| `GET /users/{userId}/expression-context` | Get user context for rule evaluation | Catalog, Cart |
| `GET /users/{userId}/access-check` | Verify user access | API Gateway, Auth |

For detailed API specifications, see [API.md](API.md#integration).

## Catalog Service Integration

The Catalog Service uses the User Management Service to determine which products should be visible to specific users.

### Integration Flow

```
1. User requests product catalog
2. Catalog Service calls User Management:
   GET /api/integration/visibility-rules?userId={userId}&companyId={companyId}
3. Catalog Service evaluates expressions against products
4. Catalog Service filters products based on rules
5. Catalog Service returns filtered catalog to user
```

### Step-by-Step Implementation

#### Step 1: Retrieve Visibility Rules

When a user requests the catalog, fetch their visibility rules:

```http
GET /api/integration/visibility-rules?userId=550e8400-e29b-41d4-a716-446655440000&companyId=770e8400-e29b-41d4-a716-446655440000
```

**Response**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "companyId": "770e8400-e29b-41d4-a716-446655440000",
  "rules": [
    {
      "ruleId": "bb0e8400-e29b-41d4-a716-446655440000",
      "conditionId": "990e8400-e29b-41d4-a716-446655440000",
      "conditionName": "Premium Access",
      "expression": "product.isPremium == true",
      "priority": 10
    },
    {
      "ruleId": "cc0e8400-e29b-41d4-a716-446655440000",
      "conditionId": "aa0e8400-e29b-41d4-a716-446655440000",
      "conditionName": "Electronics Category",
      "expression": "product.category == 'Electronics'",
      "priority": 5
    }
  ]
}
```

#### Step 2: Evaluate Rules Against Products

For each product in your catalog, evaluate all visibility rules. A product is visible if:
- No visibility rules exist (show all), OR
- At least one visibility rule evaluates to `true`

#### Step 3: Apply Priority Ordering

Rules are returned in priority order (highest priority first). Evaluate in this order and stop at the first match if using short-circuit evaluation.

### Example Implementation (C#)

```csharp
using System.Linq.Dynamic.Core; // NuGet: System.Linq.Dynamic.Core

public class CatalogService
{
    private readonly HttpClient _httpClient;
    private readonly string _userManagementBaseUrl;

    public CatalogService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _userManagementBaseUrl = config["Services:UserManagement:BaseUrl"];
    }

    public async Task<List<Product>> GetVisibleProductsAsync(
        Guid userId,
        Guid? companyId = null)
    {
        // Step 1: Fetch all products from your catalog
        var allProducts = await GetAllProductsFromDatabase();

        // Step 2: Fetch visibility rules for the user
        var visibilityRules = await GetVisibilityRulesAsync(userId, companyId);

        // Step 3: If no rules exist, all products are visible
        if (!visibilityRules.Rules.Any())
        {
            return allProducts;
        }

        // Step 4: Filter products based on rules
        var visibleProducts = new List<Product>();

        foreach (var product in allProducts)
        {
            if (IsProductVisible(product, visibilityRules.Rules))
            {
                visibleProducts.Add(product);
            }
        }

        return visibleProducts;
    }

    private async Task<VisibilityRulesDTO> GetVisibilityRulesAsync(
        Guid userId,
        Guid? companyId)
    {
        var url = $"{_userManagementBaseUrl}/api/integration/visibility-rules?userId={userId}";
        if (companyId.HasValue)
        {
            url += $"&companyId={companyId}";
        }

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<VisibilityRulesDTO>();
    }

    private bool IsProductVisible(Product product, List<VisibilityRuleDTO> rules)
    {
        // Rules are ordered by priority (highest first)
        // A product is visible if ANY rule evaluates to true

        foreach (var rule in rules)
        {
            try
            {
                // Create evaluation context
                var context = new
                {
                    product = new
                    {
                        id = product.Id,
                        name = product.Name,
                        category = product.Category,
                        price = product.Price,
                        isPremium = product.IsPremium,
                        tags = product.Tags,
                        stock = product.StockQuantity,
                        isActive = product.IsActive
                    }
                };

                // Evaluate expression using Dynamic LINQ
                var parameter = Expression.Parameter(context.GetType(), "ctx");
                var e = DynamicExpressionParser.ParseLambda(
                    new[] { parameter },
                    typeof(bool),
                    rule.Expression);

                var result = (bool)e.Compile().DynamicInvoke(context);

                if (result)
                {
                    return true; // Product matches this rule, it's visible
                }
            }
            catch (Exception ex)
            {
                // Log expression evaluation error
                _logger.LogError(ex,
                    "Error evaluating visibility rule {RuleId}: {Expression}",
                    rule.RuleId,
                    rule.Expression);
                // Continue to next rule on error
            }
        }

        // No rules matched, product is not visible
        return false;
    }
}
```

### Expression Examples

Common visibility rule expressions:

```javascript
// Show only premium products
product.isPremium == true

// Show products in specific category
product.category == "Electronics"

// Show products above certain price
product.price > 1000

// Combine multiple conditions
product.category == "Electronics" && product.price < 5000

// Check if product has specific tag
product.tags.Contains("featured")

// Check stock availability
product.stock > 0 && product.isActive == true
```

## Cart & Quote Service Integration

The Cart/Quote Service uses the User Management Service to apply discounts to products and calculate final pricing.

### Integration Flow

```
1. User adds items to cart
2. Cart Service calls User Management:
   GET /api/integration/discount-rules?userId={userId}&companyId={companyId}
3. Cart Service evaluates discount rules against cart items
4. Cart Service applies applicable discounts
5. Cart Service calculates final pricing
6. Cart Service returns cart with discounted prices
```

### Step-by-Step Implementation

#### Step 1: Retrieve Discount Rules

```http
GET /api/integration/discount-rules?userId=550e8400-e29b-41d4-a716-446655440000&companyId=770e8400-e29b-41d4-a716-446655440000
```

**Response**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "companyId": "770e8400-e29b-41d4-a716-446655440000",
  "rules": [
    {
      "ruleId": "aa0e8400-e29b-41d4-a716-446655440000",
      "conditionId": "990e8400-e29b-41d4-a716-446655440000",
      "conditionName": "Electronics Discount",
      "expression": "product.category == 'Electronics'",
      "discountType": "Percentage",
      "discountValue": 10.00,
      "priority": 10
    },
    {
      "ruleId": "dd0e8400-e29b-41d4-a716-446655440000",
      "conditionId": "bb0e8400-e29b-41d4-a716-446655440000",
      "conditionName": "Bulk Purchase",
      "expression": "product.quantity >= 10",
      "discountType": "Percentage",
      "discountValue": 5.00,
      "priority": 5
    }
  ]
}
```

#### Step 2: Evaluate and Apply Discounts

For each item in the cart:
1. Evaluate all discount rules to find applicable ones
2. Apply discounts based on your business rules:
   - **Highest discount only**: Apply only the highest applicable discount
   - **Cumulative**: Stack multiple discounts
   - **Priority-based**: Apply the first matching discount by priority

### Example Implementation (C#)

```csharp
public class CartService
{
    private readonly HttpClient _httpClient;
    private readonly string _userManagementBaseUrl;

    public async Task<Cart> ApplyDiscountsAsync(
        Cart cart,
        Guid userId,
        Guid? companyId = null)
    {
        // Fetch discount rules
        var discountRules = await GetDiscountRulesAsync(userId, companyId);

        if (!discountRules.Rules.Any())
        {
            return cart; // No discounts to apply
        }

        // Apply discounts to each item
        foreach (var item in cart.Items)
        {
            var applicableDiscounts = FindApplicableDiscounts(
                item,
                discountRules.Rules);

            if (applicableDiscounts.Any())
            {
                // Business Rule: Apply highest discount only
                var bestDiscount = applicableDiscounts
                    .OrderByDescending(d => CalculateDiscountAmount(item, d))
                    .First();

                ApplyDiscount(item, bestDiscount);
            }
        }

        // Recalculate cart totals
        cart.RecalculateTotals();

        return cart;
    }

    private async Task<DiscountRulesDTO> GetDiscountRulesAsync(
        Guid userId,
        Guid? companyId)
    {
        var url = $"{_userManagementBaseUrl}/api/integration/discount-rules?userId={userId}";
        if (companyId.HasValue)
        {
            url += $"&companyId={companyId}";
        }

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DiscountRulesDTO>();
    }

    private List<DiscountRuleDTO> FindApplicableDiscounts(
        CartItem item,
        List<DiscountRuleDTO> rules)
    {
        var applicableDiscounts = new List<DiscountRuleDTO>();

        foreach (var rule in rules)
        {
            try
            {
                // Create evaluation context
                var context = new
                {
                    product = new
                    {
                        id = item.ProductId,
                        name = item.ProductName,
                        category = item.Category,
                        price = item.UnitPrice,
                        quantity = item.Quantity,
                        subtotal = item.Quantity * item.UnitPrice
                    }
                };

                // Evaluate expression
                var parameter = Expression.Parameter(context.GetType(), "ctx");
                var e = DynamicExpressionParser.ParseLambda(
                    new[] { parameter },
                    typeof(bool),
                    rule.Expression);

                var result = (bool)e.Compile().DynamicInvoke(context);

                if (result)
                {
                    applicableDiscounts.Add(rule);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error evaluating discount rule {RuleId}: {Expression}",
                    rule.RuleId,
                    rule.Expression);
            }
        }

        // Return ordered by priority (highest first)
        return applicableDiscounts.OrderByDescending(d => d.Priority).ToList();
    }

    private decimal CalculateDiscountAmount(CartItem item, DiscountRuleDTO rule)
    {
        var itemSubtotal = item.Quantity * item.UnitPrice;

        return rule.DiscountType switch
        {
            "Percentage" => itemSubtotal * (rule.DiscountValue / 100m),
            "Fixed" => rule.DiscountValue * item.Quantity,
            _ => 0m
        };
    }

    private void ApplyDiscount(CartItem item, DiscountRuleDTO rule)
    {
        var discountAmount = CalculateDiscountAmount(item, rule);

        item.DiscountAmount = discountAmount;
        item.DiscountRuleId = rule.RuleId;
        item.DiscountDescription = rule.ConditionName;
        item.FinalPrice = (item.Quantity * item.UnitPrice) - discountAmount;
    }
}
```

### Discount Expression Examples

Common discount rule expressions:

```javascript
// Category-based discount
product.category == "Electronics"

// Minimum purchase amount
product.subtotal >= 1000

// Bulk purchase discount
product.quantity >= 10

// Price range discount
product.price >= 500 && product.price <= 2000

// Multiple conditions
product.category == "Electronics" && product.quantity >= 5

// Specific products
["PROD001", "PROD002", "PROD003"].Contains(product.id)
```

## Expression Context

The expression context provides all necessary data for evaluating rules in other services.

### Retrieving Expression Context

```http
GET /api/integration/users/{userId}/expression-context
```

**Response**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "userType": "Internal",
  "userRole": "Customer",
  "isActive": true,
  "emailVerified": true,
  "companies": [
    {
      "companyId": "770e8400-e29b-41d4-a716-446655440000",
      "cnpj": "12345678000190",
      "isActive": true
    }
  ],
  "metadata": {
    "createdAt": "2024-01-15T10:30:00Z",
    "hasAddresses": true,
    "addressCount": 2
  }
}
```

### Using Expression Context

The expression context can be used when you need to evaluate complex rules that reference user properties:

```javascript
// Rules based on user type
user.userType == "Internal"

// Rules based on user role
user.userRole == "Admin" || user.userRole == "Manager"

// Rules based on company
user.companies.Any(c => c.cnpj == "12345678000190")

// Combined user and product rules
product.isPremium == true && user.userType == "Internal"
```

## Code Examples

### Complete Integration Example (C#)

Here's a complete example showing integration from a hypothetical Catalog Service:

```csharp
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Dynamic.Core;

public class UserManagementIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserManagementIntegrationService> _logger;
    private readonly string _baseUrl;

    public UserManagementIntegrationService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<UserManagementIntegrationService> logger,
        IConfiguration config)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _baseUrl = config["Services:UserManagement:BaseUrl"];
    }

    // Get visibility rules with caching
    public async Task<VisibilityRulesDTO> GetVisibilityRulesAsync(
        Guid userId,
        Guid? companyId = null)
    {
        var cacheKey = $"visibility_rules_{userId}_{companyId}";

        if (_cache.TryGetValue(cacheKey, out VisibilityRulesDTO cachedRules))
        {
            _logger.LogDebug("Returning cached visibility rules for user {UserId}", userId);
            return cachedRules;
        }

        try
        {
            var url = $"{_baseUrl}/api/integration/visibility-rules?userId={userId}";
            if (companyId.HasValue)
            {
                url += $"&companyId={companyId}";
            }

            _logger.LogInformation("Fetching visibility rules from {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var rules = await response.Content.ReadFromJsonAsync<VisibilityRulesDTO>();

            // Cache for 5 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(cacheKey, rules, cacheOptions);

            return rules;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to fetch visibility rules for user {UserId}",
                userId);
            throw new IntegrationException(
                "Unable to fetch visibility rules from User Management Service",
                ex);
        }
    }

    // Get discount rules with caching
    public async Task<DiscountRulesDTO> GetDiscountRulesAsync(
        Guid userId,
        Guid? companyId = null)
    {
        var cacheKey = $"discount_rules_{userId}_{companyId}";

        if (_cache.TryGetValue(cacheKey, out DiscountRulesDTO cachedRules))
        {
            return cachedRules;
        }

        try
        {
            var url = $"{_baseUrl}/api/integration/discount-rules?userId={userId}";
            if (companyId.HasValue)
            {
                url += $"&companyId={companyId}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var rules = await response.Content.ReadFromJsonAsync<DiscountRulesDTO>();

            // Cache for 5 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(cacheKey, rules, cacheOptions);

            return rules;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to fetch discount rules for user {UserId}",
                userId);
            throw new IntegrationException(
                "Unable to fetch discount rules from User Management Service",
                ex);
        }
    }

    // Verify user access
    public async Task<bool> CheckUserAccessAsync(Guid userId)
    {
        var cacheKey = $"access_check_{userId}";

        if (_cache.TryGetValue(cacheKey, out bool cachedAccess))
        {
            return cachedAccess;
        }

        try
        {
            var url = $"{_baseUrl}/api/integration/users/{userId}/access-check";
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();

            var accessCheck = await response.Content
                .ReadFromJsonAsync<AccessCheckDTO>();

            var hasAccess = accessCheck.HasAccess;

            // Cache for 2 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(2));

            _cache.Set(cacheKey, hasAccess, cacheOptions);

            return hasAccess;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to check access for user {UserId}",
                userId);
            // Fail closed - deny access on error
            return false;
        }
    }
}

// Custom exception for integration errors
public class IntegrationException : Exception
{
    public IntegrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

### DTOs for Integration

```csharp
// Visibility Rules DTO
public class VisibilityRulesDTO
{
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public List<VisibilityRuleDTO> Rules { get; set; } = new();
}

public class VisibilityRuleDTO
{
    public Guid RuleId { get; set; }
    public Guid ConditionId { get; set; }
    public string ConditionName { get; set; }
    public string Expression { get; set; }
    public int Priority { get; set; }
}

// Discount Rules DTO
public class DiscountRulesDTO
{
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public List<DiscountRuleDTO> Rules { get; set; } = new();
}

public class DiscountRuleDTO
{
    public Guid RuleId { get; set; }
    public Guid ConditionId { get; set; }
    public string ConditionName { get; set; }
    public string Expression { get; set; }
    public string DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public int Priority { get; set; }
}

// Access Check DTO
public class AccessCheckDTO
{
    public Guid UserId { get; set; }
    public bool HasAccess { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public bool EmailVerified { get; set; }
    public string UserType { get; set; }
    public string UserRole { get; set; }
    public string Message { get; set; }
}
```

## Best Practices

### 1. Implement Caching

Commercial conditions don't change frequently. Cache responses to reduce load:

```csharp
// Cache for 5-10 minutes
var cacheOptions = new MemoryCacheEntryOptions()
    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
```

### 2. Handle Errors Gracefully

```csharp
try
{
    var rules = await GetVisibilityRulesAsync(userId, companyId);
    // Use rules
}
catch (IntegrationException ex)
{
    _logger.LogError(ex, "Integration failed");
    // Fallback: Show default catalog without filtering
    return await GetDefaultCatalog();
}
```

### 3. Use Circuit Breaker Pattern

Protect against cascading failures:

```csharp
// Using Polly
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)
    );
```

### 4. Implement Retry Logic

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
    );
```

### 5. Log Integration Calls

```csharp
_logger.LogInformation(
    "Fetching visibility rules for User {UserId}, Company {CompanyId}",
    userId,
    companyId);
```

### 6. Validate Expressions

```csharp
private bool IsExpressionValid(string expression)
{
    try
    {
        // Try parsing the expression
        var parameter = Expression.Parameter(typeof(object), "ctx");
        DynamicExpressionParser.ParseLambda(
            new[] { parameter },
            typeof(bool),
            expression);
        return true;
    }
    catch
    {
        return false;
    }
}
```

### 7. Monitor Integration Health

Track metrics:
- Response times
- Error rates
- Cache hit rates
- Expression evaluation failures

### 8. Use Bulk Operations

When processing multiple users/products, make parallel requests:

```csharp
var tasks = userIds.Select(userId =>
    GetVisibilityRulesAsync(userId, companyId));

var results = await Task.WhenAll(tasks);
```

## Troubleshooting

### Common Issues

#### 1. Empty Rules Returned

**Problem**: No rules are returned for a user.

**Possible Causes**:
- User has no associated companies
- No commercial conditions assigned to user's companies
- All conditions are inactive or expired

**Solution**: Verify user-company associations and commercial condition assignments.

#### 2. Expression Evaluation Errors

**Problem**: Expressions fail to evaluate.

**Possible Causes**:
- Invalid expression syntax
- Missing properties in evaluation context
- Type mismatches

**Solution**:
- Validate expression syntax before saving
- Provide complete context with all expected properties
- Log evaluation errors for debugging

#### 3. Performance Issues

**Problem**: Slow response times.

**Possible Causes**:
- No caching implemented
- Too many rules to evaluate
- Network latency

**Solution**:
- Implement caching with appropriate TTL
- Optimize rule evaluation logic
- Consider pagination for large rule sets

#### 4. 404 User Not Found

**Problem**: User lookup fails.

**Possible Causes**:
- User doesn't exist
- User is soft-deleted
- Wrong user ID

**Solution**: Verify user exists and is active before integration calls.

### Debugging Tips

1. **Enable detailed logging**:
```csharp
builder.Services.AddHttpClient<UserManagementIntegrationService>()
    .AddLogger();
```

2. **Test expressions manually**:
```csharp
var testContext = new { product = new { price = 1000 } };
var result = EvaluateExpression("product.price > 500", testContext);
```

3. **Verify network connectivity**:
```bash
curl http://user-management-service/health
```

4. **Check service logs** for errors and warnings.

## Support

For integration issues:
- Review this guide
- Check API documentation: [API.md](API.md)
- Review service logs
- Contact: github@fgbrind.es
