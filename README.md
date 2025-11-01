# User Management Service

A comprehensive user and company management microservice for the FG Brindes e-commerce platform, built with .NET 9.0 and designed for deployment on AWS ECS.

## Overview

The User Management Service is a core microservice responsible for managing users, companies, and their associated commercial conditions in the FG Brindes e-commerce ecosystem. It provides robust APIs for user registration, company management, and dynamic commercial rule configuration that integrates seamlessly with other platform services.

## Key Features

### User Management
- **Self-Registration & Internal Users**: Support for both customer self-registration and internal user creation by administrators
- **User Profiles**: Complete profile management with addresses, contact information, and documents (CPF/CNPJ)
- **User Roles**: Flexible role-based user classification (Customer, Admin, Manager, etc.)
- **Activation Control**: Enable/disable user accounts with activation and deactivation workflows
- **Email-based Lookup**: Fast user retrieval by email address with unique email constraints

### Company Management
- **Company Profiles**: Complete company registration with Brazilian tax information (CNPJ, State/Municipal Registration)
- **Multi-Address Support**: Multiple addresses per company (billing, shipping, commercial)
- **User Associations**: Many-to-many relationship between users and companies
- **Company Activation**: Activate/deactivate company accounts independently from users
- **CNPJ Validation**: Unique CNPJ constraint for company identification

### Commercial Conditions
- **Dynamic Rule Engine**: Flexible expression-based rules for product visibility and pricing
- **Visibility Rules**: Control which products are visible to specific users/companies
- **Discount Rules**: Define percentage or fixed-value discounts based on conditions
- **Priority System**: Multiple conditions evaluated by priority order
- **Time-based Validity**: Optional validity periods for temporary promotions
- **Company Assignment**: Assign multiple commercial conditions to companies

### Integration APIs
- **Service-to-Service Endpoints**: Specialized APIs designed for microservice communication
- **Commercial Conditions Aggregation**: Retrieve all applicable conditions for a user/company context
- **Visibility Rules Export**: Get all visibility rules for catalog filtering
- **Discount Rules Export**: Get all discount rules for cart/quote calculations
- **Expression Context**: Provide user context data for rule evaluation
- **Access Control**: User access verification for authentication services

## Technology Stack

### Core Framework
- **.NET 9.0**: Latest .NET runtime with enhanced performance and features
- **ASP.NET Core 9.0**: Web API framework with OpenAPI support
- **Entity Framework Core 9.0**: ORM for database operations and migrations

### Database
- **PostgreSQL 16**: Production database (AWS RDS compatible)
- **SQLite**: Development database for local development
- **Npgsql**: PostgreSQL provider for Entity Framework Core

### API Documentation
- **Swagger/OpenAPI**: Interactive API documentation and testing interface
- **XML Documentation**: Comprehensive inline documentation for all endpoints

### Monitoring & Observability
- **Application Insights**: Azure Application Insights integration for telemetry
- **Health Checks**: Built-in health endpoints for ECS health monitoring
- **Structured Logging**: Comprehensive logging with Microsoft.Extensions.Logging

### Deployment
- **Docker**: Multi-stage Dockerfile for optimized container images
- **AWS ECS**: Elastic Container Service for production deployment
- **GitHub Actions**: CI/CD pipeline for automated deployment
- **AWS ECR**: Elastic Container Registry for Docker image storage

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     User Management API                      │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Users      │  │  Companies   │  │ Commercial   │      │
│  │  Controller  │  │  Controller  │  │ Conditions   │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                  │                  │               │
│  ┌──────▼──────────────────▼──────────────────▼───────┐     │
│  │              Integration Controller                 │     │
│  │  (Service-to-Service Communication Endpoints)      │     │
│  └──────────────────────┬──────────────────────────────┘     │
│                         │                                     │
│  ┌──────────────────────▼──────────────────────────────┐     │
│  │                  Service Layer                       │     │
│  │  - UserService    - CompanyService                  │     │
│  │  - CommercialConditionService                       │     │
│  │  - IntegrationService                               │     │
│  └──────────────────────┬──────────────────────────────┘     │
│                         │                                     │
│  ┌──────────────────────▼──────────────────────────────┐     │
│  │               Repository Layer                       │     │
│  │  - Generic Repository Pattern                       │     │
│  │  - Specialized Repositories                         │     │
│  └──────────────────────┬──────────────────────────────┘     │
│                         │                                     │
│  ┌──────────────────────▼──────────────────────────────┐     │
│  │          Entity Framework Core DbContext            │     │
│  └──────────────────────┬──────────────────────────────┘     │
│                         │                                     │
└─────────────────────────┼─────────────────────────────────────┘
                          │
                   ┌──────▼──────┐
                   │  PostgreSQL │
                   │   Database  │
                   └─────────────┘

External Service Integration:
┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│  Catalog Service │────▶│ Integration APIs │◀────│ Cart/Quote       │
│  (Visibility)    │     │                  │     │ Service          │
└──────────────────┘     └──────────────────┘     │ (Discounts)      │
                                                   └──────────────────┘
```

## Prerequisites

### Development Environment
- **.NET SDK 9.0** or later ([Download](https://dotnet.microsoft.com/download))
- **PostgreSQL 16** (for production-like development) or SQLite (auto-included)
- **Docker Desktop** (optional, for containerized development)
- **Git** for version control

### Production Deployment
- **AWS Account** with ECS and ECR access
- **AWS CLI** configured with appropriate credentials
- **GitHub Repository** with Actions enabled (for CI/CD)

### Recommended Tools
- **Visual Studio 2022** or **VS Code** with C# extension
- **Postman** or similar tool for API testing
- **pgAdmin** or **DBeaver** for database management

## Getting Started

### Local Development (SQLite)

1. **Clone the repository**
```bash
git clone <repository-url>
cd ecomm-user-management-service
```

2. **Restore dependencies**
```bash
cd src/UserManagementAPI
dotnet restore
```

3. **Run the application**
```bash
dotnet run
```

The API will be available at `http://localhost:5000` (or the port shown in the console).
Swagger UI will be accessible at `http://localhost:5000/swagger`

4. **Database initialization**
The application automatically creates the SQLite database on first run using `EnsureCreated()` in development mode.

### Local Development (PostgreSQL with Docker)

1. **Start PostgreSQL with Docker Compose**
```bash
docker-compose up -d postgres
```

2. **Set environment to Production mode** (to use PostgreSQL)
```bash
export ASPNETCORE_ENVIRONMENT=Production
export DATABASE_URL="Host=localhost;Port=5432;Database=usermanagement;Username=postgres;Password=postgres"
```

3. **Apply migrations**
```bash
cd src/UserManagementAPI
dotnet ef database update
```

4. **Run the application**
```bash
dotnet run
```

### Docker Development

Run the entire stack with Docker Compose:

```bash
docker-compose up
```

This starts:
- PostgreSQL database on port 5432
- User Management API on port 8080

Access the API at `http://localhost:8080`

### Database Migrations

#### Create a new migration
```bash
cd src/UserManagementAPI
dotnet ef migrations add MigrationName
```

#### Apply migrations
```bash
dotnet ef database update
```

#### Rollback to specific migration
```bash
dotnet ef database update PreviousMigrationName
```

#### Remove last migration (if not applied)
```bash
dotnet ef migrations remove
```

## API Documentation

### Swagger UI

Interactive API documentation is available at the root URL when running in development mode:
- **Local**: `http://localhost:5000/`
- **Docker**: `http://localhost:8080/`

### API Endpoints Overview

The API is organized into five main controllers:

- **Users** (`/api/users`) - User management and profiles
- **Companies** (`/api/companies`) - Company management and user associations
- **Commercial Conditions** (`/api/commercialconditions`) - Rules and conditions management
- **Integration** (`/api/integration`) - Service-to-service communication endpoints
- **Health** (`/health`) - Health check endpoint for monitoring

For detailed API documentation, see [docs/API.md](docs/API.md)

## Deployment

### AWS ECS Deployment

#### Prerequisites
1. Configure AWS credentials:
```bash
aws configure
```

2. Set up GitHub Secrets for CI/CD:
   - `AWS_ACCESS_KEY_ID`
   - `AWS_SECRET_ACCESS_KEY`
   - `AWS_REGION`
   - `DATABASE_URL`

#### Initial Setup

1. **Create the ECS infrastructure** (first-time only):
```bash
cd aws
./create-service.sh
```

This script creates:
- ECR repository for Docker images
- ECS cluster
- Task definition
- ECS service with load balancer

2. **Deploy updates**:
```bash
./deploy.sh
```

#### GitHub Actions CI/CD

The repository includes a GitHub Actions workflow that automatically:
1. Builds the Docker image on push to `main` branch
2. Pushes the image to AWS ECR
3. Updates the ECS task definition
4. Deploys the new version to ECS

Workflow file: `.github/workflows/deploy-to-aws.yml`

### Manual Docker Deployment

1. **Build the Docker image**:
```bash
docker build -t user-management-api .
```

2. **Run the container**:
```bash
docker run -d \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DATABASE_URL="your-postgresql-connection-string" \
  -e CORS_ORIGINS="https://yourdomain.com" \
  --name user-management-api \
  user-management-api
```

## Environment Variables

### Required in Production

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `DATABASE_URL` | PostgreSQL connection string | `Host=db.example.com;Port=5432;Database=usermanagement;Username=user;Password=pass` |

### Optional Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `CORS_ORIGINS` | Allowed CORS origins (comma-separated) | `*` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights connection string | None |
| `ConnectionStrings__PostgresConnection` | Alternative PostgreSQL connection | None |
| `ConnectionStrings__SqliteConnection` | SQLite connection (dev only) | `Data Source=usermanagement.db` |

### Environment-specific Files

Configuration is managed through:
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production overrides
- Environment variables (highest priority)

## Project Structure

```
ecomm-user-management-service/
├── src/
│   └── UserManagementAPI/
│       ├── Controllers/           # API endpoint controllers
│       │   ├── UsersController.cs
│       │   ├── CompaniesController.cs
│       │   ├── CommercialConditionsController.cs
│       │   ├── IntegrationController.cs
│       │   └── HealthController.cs
│       ├── Services/              # Business logic layer
│       │   ├── UserService.cs
│       │   ├── CompanyService.cs
│       │   ├── CommercialConditionService.cs
│       │   └── IntegrationService.cs
│       ├── Repositories/          # Data access layer
│       │   ├── Repository.cs (Generic)
│       │   ├── UserRepository.cs
│       │   ├── CompanyRepository.cs
│       │   └── CommercialConditionRepository.cs
│       ├── Models/                # Domain models
│       │   ├── Entities/          # Database entities
│       │   │   ├── User.cs
│       │   │   ├── Company.cs
│       │   │   ├── CommercialCondition.cs
│       │   │   ├── ConditionRule.cs
│       │   │   ├── Address.cs
│       │   │   ├── CompanyUser.cs
│       │   │   └── CompanyCommercialCondition.cs
│       │   └── Enums/             # Enumerations
│       │       ├── UserType.cs
│       │       ├── UserRole.cs
│       │       ├── AddressType.cs
│       │       ├── RuleType.cs
│       │       └── DiscountType.cs
│       ├── DTOs/                  # Data transfer objects
│       │   ├── Users/
│       │   ├── Companies/
│       │   ├── CommercialConditions/
│       │   ├── Integration/
│       │   └── Common/
│       ├── Data/                  # Database context
│       │   └── UserManagementDbContext.cs
│       ├── Migrations/            # EF Core migrations
│       ├── Program.cs             # Application entry point
│       └── appsettings.json       # Configuration
├── aws/                           # AWS deployment scripts
│   ├── create-service.sh
│   ├── deploy.sh
│   ├── task-definition.json
│   └── service-definition.json
├── .github/
│   └── workflows/
│       └── deploy-to-aws.yml      # CI/CD pipeline
├── docs/                          # Documentation
│   ├── API.md                     # API documentation
│   ├── INTEGRATION.md             # Integration guide
│   └── DATABASE.md                # Database schema
├── Dockerfile                     # Docker configuration
├── docker-compose.yml             # Local development setup
├── UserManagementAPI.sln          # Solution file
└── README.md                      # This file
```

## Development Guidelines

### Code Organization
- **Controllers**: Thin controllers focused on HTTP concerns
- **Services**: Business logic and orchestration
- **Repositories**: Data access abstraction
- **DTOs**: Request/response models separate from entities

### Naming Conventions
- **Controllers**: `{Entity}Controller` (e.g., `UsersController`)
- **Services**: `{Entity}Service` implementing `I{Entity}Service`
- **Repositories**: `{Entity}Repository` implementing `I{Entity}Repository`
- **DTOs**: `{Action}{Entity}DTO` (e.g., `CreateUserDTO`, `UserDetailDTO`)

### Best Practices
1. **Always use DTOs** for API requests/responses, never expose entities directly
2. **Implement proper error handling** with try-catch blocks in controllers
3. **Use structured logging** with meaningful context
4. **Write XML documentation** for all public APIs
5. **Follow async/await patterns** for all I/O operations
6. **Apply migrations** before deploying to production

## Testing

### Manual Testing with Swagger

1. Start the application
2. Navigate to the Swagger UI
3. Explore and test endpoints interactively

### Testing with Postman

Import the OpenAPI specification from `/swagger/v1/swagger.json` to generate a Postman collection.

### Health Check

The health endpoint can be tested with:
```bash
curl http://localhost:8080/health
```

Expected response: `Healthy`

## Integration with Other Services

This service provides specialized integration endpoints for other microservices in the e-commerce platform. See [docs/INTEGRATION.md](docs/INTEGRATION.md) for detailed integration guide.

### Catalog Service Integration
- Retrieve visibility rules for product filtering
- Get expression context for rule evaluation

### Cart/Quote Service Integration
- Retrieve discount rules for pricing calculations
- Get user/company commercial conditions

### Authentication Service Integration
- User access checks
- User profile retrieval by email

## Contributing

### Workflow

1. Create a feature branch from `main`
2. Make your changes
3. Test locally
4. Create a pull request
5. Wait for CI/CD checks to pass
6. Request code review

### Commit Messages

Follow conventional commit format:
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `refactor:` Code refactoring
- `test:` Test additions/changes
- `chore:` Build/tooling changes

### Pull Request Guidelines

- Provide clear description of changes
- Include any necessary migration scripts
- Update documentation if API changes
- Ensure all tests pass
- Keep PRs focused and reasonably sized

## License

Copyright (c) 2024 FG Brindes. All rights reserved.

This software is proprietary and confidential. Unauthorized copying, distribution, or use of this software, via any medium, is strictly prohibited.

## Support

For issues, questions, or contributions:
- **Email**: github@fgbrind.es
- **GitHub Issues**: Use the repository issue tracker

## Changelog

### Version 1.0.0 (2024-11-01)
- Initial release
- User management functionality
- Company management functionality
- Commercial conditions and rules engine
- Integration APIs for microservices
- AWS ECS deployment support
- PostgreSQL and SQLite database support
