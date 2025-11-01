# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9.0 microservice for managing users, companies, and commercial conditions (discount/visibility rules) for the FG Brindes e-commerce platform. It's deployed on AWS ECS and uses PostgreSQL in production, SQLite in development.

## Essential Commands

### Development Workflow

```bash
# Navigate to project directory
cd src/UserManagementAPI

# Restore dependencies
dotnet restore

# Run locally (SQLite - no database setup needed)
dotnet run
# Access Swagger UI at http://localhost:5000

# Run with PostgreSQL
export ASPNETCORE_ENVIRONMENT=Production
export DATABASE_URL="Host=localhost;Port=5432;Database=usermanagement;Username=postgres;Password=postgres"
dotnet run

# Build
dotnet build

# Build solution from root
dotnet build UserManagementAPI.sln
```

### Database Migrations

```bash
cd src/UserManagementAPI

# Create migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not yet applied)
dotnet ef migrations remove
```

### Docker Operations

```bash
# Run full stack (PostgreSQL + API)
docker-compose up

# Run only PostgreSQL (for local .NET development)
docker-compose up -d postgres

# Build Docker image
docker build -t user-management-api .

# Run container
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DATABASE_URL="connection-string" \
  user-management-api
```

### Testing

```bash
# Manual testing via Swagger UI
# Navigate to http://localhost:5000 (or http://localhost:8080 with Docker)

# Health check
curl http://localhost:8080/health
```

## Architecture

### Layered Architecture

The codebase follows a clean layered architecture:

```
Controllers (HTTP layer)
    ↓ calls
Services (Business logic)
    ↓ calls
Repositories (Data access)
    ↓ uses
DbContext (EF Core)
    ↓ queries
Database (PostgreSQL/SQLite)
```

**Key principle**: Controllers are thin and only handle HTTP concerns. All business logic lives in Services. Never expose domain entities directly - always use DTOs.

### Core Domain Model

The database is organized around 8 main entities:

1. **User** - Customer/employee accounts with addresses
2. **Company** - Business entities with Brazilian tax info (CNPJ)
3. **CompanyUser** - Many-to-many join table for user-company associations
4. **Address** - Physical addresses (linked to users or companies)
5. **CommercialCondition** - Business rules with priority and time validity
6. **ConditionRule** - Individual rules (discount or visibility type)
7. **CompanyCommercialCondition** - Many-to-many join for company-condition assignments
8. **AuditLog** - Audit trail for changes

### Important Relationships

- Users can belong to multiple Companies (many-to-many via CompanyUser)
- Companies can have multiple CommercialConditions (many-to-many via CompanyCommercialCondition)
- Each CommercialCondition contains multiple ConditionRules
- Users and Companies both have multiple Addresses

## Key Implementation Details

### Database Context Configuration

**Location**: [src/UserManagementAPI/Data/UserManagementDbContext.cs](src/UserManagementAPI/Data/UserManagementDbContext.cs)

The DbContext contains extensive model configuration:
- All entity relationships are explicitly configured in `OnModelCreating`
- Composite keys for join tables (CompanyUser, CompanyCommercialCondition)
- Multiple indexes for performance (Email, CNPJ, IsActive, Priority, etc.)
- Unique constraints on Email and CNPJ
- Cascade delete rules for relationships

### Database Connection Logic

**Location**: [src/UserManagementAPI/Program.cs:15-46](src/UserManagementAPI/Program.cs#L15-L46)

**Production mode** (ASPNETCORE_ENVIRONMENT=Production):
- Uses PostgreSQL with connection pooling
- Connection string priority: `DATABASE_URL` env var → `ConnectionStrings__PostgresConnection` env var → appsettings
- Retry logic: 5 retries with 10s max delay
- Migrations applied automatically on startup

**Development mode**:
- Uses SQLite with `EnsureCreated()` (no migrations needed)
- Database file: `usermanagement.db` in project root
- No connection string required

### Repository Pattern

**Generic Repository**: [src/UserManagementAPI/Repositories/Repository.cs](src/UserManagementAPI/Repositories/Repository.cs)
- Base CRUD operations for all entities
- `GetAllAsync()`, `GetByIdAsync()`, `AddAsync()`, `UpdateAsync()`, `DeleteAsync()`

**Specialized Repositories**:
- UserRepository: `GetByEmailAsync()`, `GetByIdWithAddressesAsync()`, `GetByIdWithCompaniesAsync()`
- CompanyRepository: `GetByCnpjAsync()`, `GetByIdWithUsersAsync()`, `GetByIdWithCommercialConditionsAsync()`
- CommercialConditionRepository: `GetActiveConditionsAsync()`, `GetByIdWithRulesAsync()`, `GetConditionsByCompanyIdAsync()`
- ConditionRuleRepository: `GetRulesByConditionIdAsync()`, `GetRulesByTypeAsync()`

**Key pattern**: Use `Include()` and `ThenInclude()` in specialized repository methods for eager loading related entities.

### Service Layer

**Pattern**: Services orchestrate business logic and call repositories. Never call DbContext directly from services - always go through repositories.

**Key services**:
- **UserService**: User CRUD, activation/deactivation, pagination
- **CompanyService**: Company CRUD, user associations
- **CommercialConditionService**: Condition/rule management, company assignments
- **IntegrationService**: Aggregated data for microservice consumers (catalog, cart services)

### DTO Pattern

**Structure**: [src/UserManagementAPI/DTOs/](src/UserManagementAPI/DTOs/)

DTOs are organized by feature area (Users/, Companies/, CommercialConditions/, Integration/, Common/).

**Naming convention**:
- `Create{Entity}DTO` - Request for creating new entity
- `Update{Entity}DTO` - Request for updating entity
- `{Entity}DTO` - Basic response
- `{Entity}DetailDTO` - Response with related entities
- `{Entity}SummaryDTO` - Minimal response

**Important**: All DTOs use `JsonIgnoreCondition.WhenWritingNull` and enums are serialized as strings.

### Integration APIs

**Controller**: [src/UserManagementAPI/Controllers/IntegrationController.cs](src/UserManagementAPI/Controllers/IntegrationController.cs)

These endpoints are designed for service-to-service communication:

- `GET /api/integration/users/{userId}/commercial-conditions` - Get all applicable conditions for a user
- `GET /api/integration/visibility-rules` - Get all visibility rules (for Catalog Service)
- `GET /api/integration/discount-rules` - Get all discount rules (for Cart/Quote Service)
- `GET /api/integration/users/{userId}/expression-context` - Get user context for rule evaluation
- `GET /api/integration/users/{userId}/access-check` - Verify user exists and is active

**Key insight**: Commercial conditions have Priority fields and are evaluated in priority order. Rules within conditions also have priorities.

### CORS Configuration

**Location**: [src/UserManagementAPI/Program.cs:51-74](src/UserManagementAPI/Program.cs#L51-L74)

CORS origins are configured via `CORS_ORIGINS` environment variable (comma-separated) or appsettings. Default is "*" (allow all).

When specific origins are configured, credentials are allowed and custom headers are exposed: `X-Total-Count`, `X-Page`, `X-Page-Size`.

### Pagination

**DTO**: [src/UserManagementAPI/DTOs/Common/PaginatedResultDTO.cs](src/UserManagementAPI/DTOs/Common/PaginatedResultDTO.cs)

Response format:
```json
{
  "data": [...],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalCount": 45,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

## Important Configuration

### Environment Variables

**Production (required)**:
- `ASPNETCORE_ENVIRONMENT=Production`
- `DATABASE_URL` - PostgreSQL connection string (primary method)

**Optional**:
- `ConnectionStrings__PostgresConnection` - Alternative PostgreSQL connection
- `CORS_ORIGINS` - Comma-separated allowed origins (default: `*`)
- `APPLICATIONINSIGHTS_CONNECTION_STRING` - Azure Application Insights telemetry
- `ASPNETCORE_URLS` - Kestrel binding (default: `http://+:5000`, Docker uses `http://+:8080`)

### Configuration Files

- `appsettings.json` - Base config (defaults to PostgreSQL)
- `appsettings.Development.json` - SQLite override for local dev
- `appsettings.Production.json` - Production overrides
- Environment variables have highest priority

## Deployment

### AWS ECS Architecture

- **Cluster**: `fg-cluster-ecs`
- **Service**: `ecomm-user-management-service`
- **Launch Type**: FARGATE (serverless)
- **Health Check**: `/health` endpoint with 120s grace period
- **Load Balancer**: ALB with target group

### CI/CD Pipeline

**File**: [.github/workflows/deploy-to-aws.yml](.github/workflows/deploy-to-aws.yml)

**Triggers**:
- Push to `main` branch (automatic deployment)
- Manual workflow dispatch (select environment)

**Pipeline stages**:
1. Build Docker image (multi-stage with SDK 9.0 → Runtime 9.0)
2. Security scan with Trivy
3. Push to AWS ECR with git SHA and `latest` tags
4. Update ECS task definition
5. Deploy to ECS with rollback on failure
6. Health check verification

**Required GitHub Secrets**:
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_REGION`
- `DATABASE_URL`

### Manual Deployment Scripts

```bash
# Initial infrastructure setup (first time only)
cd aws
./create-service.sh

# Deploy updates
./deploy.sh
```

## Development Guidelines

### Making Changes

1. **Adding a new entity**:
   - Create entity in `Models/Entities/`
   - Add DbSet to `UserManagementDbContext`
   - Configure relationships in `OnModelCreating`
   - Create migration: `dotnet ef migrations add AddEntity`
   - Create repository interface and implementation in `Repositories/`
   - Create service interface and implementation in `Services/`
   - Register in DI container in `Program.cs`
   - Create DTOs in `DTOs/`
   - Create controller in `Controllers/`

2. **Modifying database schema**:
   - Update entity in `Models/Entities/`
   - Create migration: `dotnet ef migrations add DescriptiveChange`
   - Review generated migration code
   - Test locally with `dotnet ef database update`
   - Commit migration file

3. **Adding a new API endpoint**:
   - Add method to appropriate Service
   - Add repository method if needed
   - Add controller action with proper HTTP verb attribute
   - Add XML documentation comments
   - Create/update DTOs
   - Test via Swagger UI

### Code Standards

- **Controllers**: Thin controllers, use try-catch for error handling, return appropriate status codes
- **Services**: All business logic goes here, never expose repository methods directly to controllers
- **Repositories**: Use async/await, leverage EF Core's `Include()` for eager loading
- **DTOs**: Always use DTOs for API contracts, never return entity types directly
- **Logging**: Use structured logging with `ILogger<T>`, include context in log messages
- **Error handling**: Log exceptions with full stack trace, return user-friendly error messages

### Naming Conventions

- Controllers: `{Entity}Controller` (e.g., `UsersController`)
- Services: `{Entity}Service` implementing `I{Entity}Service`
- Repositories: `{Entity}Repository` implementing `I{Entity}Repository`
- DTOs: `{Action}{Entity}DTO` (e.g., `CreateUserDTO`, `UpdateUserDTO`)
- Endpoints: Use RESTful conventions (`GET /api/users/{id}`, `POST /api/users`)

### Testing Approach

Currently testing is done via:
- **Swagger UI**: Interactive endpoint testing in development
- **Postman**: Import OpenAPI spec from `/swagger/v1/swagger.json`
- **Health checks**: `curl http://localhost:8080/health`

When adding unit tests (recommended for future):
- Test Services with mocked repositories
- Test repository methods with in-memory database
- Test controllers with mocked services

## Common Development Scenarios

### Scenario: Adding a new commercial rule type

1. Add enum value to `Models/Enums/RuleType.cs`
2. Update `ConditionRule` entity if new fields needed
3. Create migration if schema changed
4. Update `CommercialConditionService` to handle new rule type
5. Update `IntegrationService` to expose new rule type
6. Update XML documentation and test via Swagger

### Scenario: Adding indexes for performance

1. Update `OnModelCreating` in `UserManagementDbContext.cs`
2. Add index: `modelBuilder.Entity<Entity>().HasIndex(e => e.Property)`
3. Create migration: `dotnet ef migrations add AddIndexOnProperty`
4. Review generated SQL in migration
5. Test migration locally before deploying

### Scenario: Changing database connection in Docker

```bash
# For docker-compose, edit docker-compose.yml environment section
# For manual container:
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DATABASE_URL="Host=your-db;Port=5432;Database=usermanagement;Username=user;Password=pass" \
  user-management-api
```

## Troubleshooting

### "PostgreSQL connection string not found"
- Ensure `ASPNETCORE_ENVIRONMENT=Production` is set
- Provide `DATABASE_URL` environment variable
- Or provide `ConnectionStrings__PostgresConnection` environment variable

### Swagger UI not available
- Swagger only works in Development mode
- Set `ASPNETCORE_ENVIRONMENT=Development` or access production API via direct HTTP requests

### Migration failures
- Ensure database is accessible
- Check connection string is correct
- For SQLite: Delete `usermanagement.db` and let it recreate
- For PostgreSQL: Check logs for specific error

### Docker health check failing
- Check if `/health` endpoint is responding: `curl http://localhost:8080/health`
- Check database connectivity (service depends on DB health)
- Check logs: `docker logs user-management-api`

## Additional Documentation

- [docs/API.md](docs/API.md) - Complete API endpoint documentation with examples
- [docs/INTEGRATION.md](docs/INTEGRATION.md) - Integration patterns for consuming services
- [docs/DATABASE.md](docs/DATABASE.md) - Database schema and design details
- [README.md](README.md) - Project overview and setup instructions
