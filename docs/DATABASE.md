# Database Documentation

This document provides comprehensive documentation for the User Management Service database schema, including entity relationships, tables, indexes, and migration management.

## Table of Contents

- [Overview](#overview)
- [Database Technologies](#database-technologies)
- [Entity Relationship Diagram](#entity-relationship-diagram)
- [Tables](#tables)
- [Indexes](#indexes)
- [Migrations](#migrations)
- [Seed Data](#seed-data)
- [Query Examples](#query-examples)
- [Maintenance](#maintenance)

## Overview

The User Management Service uses a relational database to store user accounts, company information, and commercial conditions with their associated rules. The schema is designed to support:

- Multi-tenant user management (self-registered and internal users)
- Company profiles with Brazilian tax information
- Many-to-many relationships between users and companies
- Flexible commercial conditions with expression-based rules
- Soft deletes and audit trails

### Schema Statistics

- **Total Tables**: 8
- **Core Entities**: 5 (User, Company, CommercialCondition, Address, ConditionRule)
- **Junction Tables**: 2 (CompanyUser, CompanyCommercialCondition)
- **Audit Tables**: 1 (AuditLog)
- **Total Indexes**: ~25 (including unique constraints)

## Database Technologies

### Production Database

**PostgreSQL 16**
- High-performance, ACID-compliant relational database
- Supports complex queries and JSON data types
- Deployed on AWS RDS for scalability and reliability

### Development Database

**SQLite**
- Lightweight, file-based database
- Zero configuration for local development
- Automatically created on first run

### ORM

**Entity Framework Core 9.0**
- Code-first migrations
- LINQ query support
- Automatic change tracking

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        User Management Schema                            │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐              ┌──────────────────┐
│      User        │              │     Company      │
├──────────────────┤              ├──────────────────┤
│ Id (PK)          │              │ Id (PK)          │
│ Name             │              │ Cnpj (UQ)        │
│ Email (UQ)       │              │ CorporateName    │
│ Phone            │              │ TradeName        │
│ Document         │              │ StateReg         │
│ UserType         │              │ MunicipalReg     │
│ Role             │              │ IsActive         │
│ IsActive         │              │ CreatedAt        │
│ EmailVerified    │              │ UpdatedAt        │
│ CreatedAt        │              │ CreatedByUserId  │
│ UpdatedAt        │              └────────┬─────────┘
│ CreatedByUserId  │                       │
└────────┬─────────┘                       │
         │                                 │
         │         ┌──────────────────┐    │
         └────────▶│  CompanyUser     │◀───┘
                   ├──────────────────┤
                   │ Id (PK)          │
                   │ CompanyId (FK)   │
                   │ UserId (FK)      │
                   │ IsActive         │
                   │ CreatedAt        │
                   │ UpdatedAt        │
                   └──────────────────┘

┌──────────────────┐                ┌──────────────────────────────┐
│     Address      │                │   CommercialCondition        │
├──────────────────┤                ├──────────────────────────────┤
│ Id (PK)          │                │ Id (PK)                      │
│ Type             │                │ Name                         │
│ PostalCode       │                │ Description                  │
│ Street           │                │ ValidFrom                    │
│ Number           │                │ ValidUntil                   │
│ Complement       │                │ Priority                     │
│ Neighborhood     │                │ IsActive                     │
│ City             │                │ CreatedAt                    │
│ State            │                │ UpdatedAt                    │
│ IsDefault        │                │ CreatedByUserId              │
│ IsActive         │                └─────────┬────────────────────┘
│ UserId (FK)      │                          │
│ CompanyId (FK)   │                          │
└──────────────────┘          ┌───────────────┼───────────────┐
    ▲         ▲               │               │               │
    │         │               │               │               │
    │         │               ▼               ▼               ▼
┌───┴───┐ ┌───┴───┐  ┌────────────────┐ ┌──────────────┐
│ User  │ │Company│  │ ConditionRule  │ │ CompanyComm  │
│       │ │       │  ├────────────────┤ │ercialCond    │
└───────┘ └───────┘  │ Id (PK)        │ ├──────────────┤
                     │ CommCondId(FK) │ │ Id (PK)      │
                     │ RuleType       │ │ CompanyId(FK)│
                     │ Expression     │ │ CommCondId   │
                     │ DiscountType   │ │ (FK)         │
                     │ DiscountValue  │ │ IsActive     │
                     │ Description    │ │ CreatedAt    │
                     │ Priority       │ │ UpdatedAt    │
                     │ IsActive       │ └──────────────┘
                     │ CreatedAt      │
                     │ UpdatedAt      │
                     └────────────────┘

┌──────────────────────────────┐
│        AuditLog              │
├──────────────────────────────┤
│ Id (PK)                      │
│ EntityType                   │
│ EntityId                     │
│ Action                       │
│ Changes                      │
│ PerformedAt                  │
│ PerformedByUserId            │
└──────────────────────────────┘
```

### Relationship Types

- **User ↔ Company**: Many-to-Many via CompanyUser
- **User → Address**: One-to-Many
- **Company → Address**: One-to-Many
- **Company ↔ CommercialCondition**: Many-to-Many via CompanyCommercialCondition
- **CommercialCondition → ConditionRule**: One-to-Many

## Tables

### Users

Stores user account information.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | GUID | No | NewGuid() | Primary key |
| Name | VARCHAR(200) | No | - | User's full name |
| Email | VARCHAR(200) | No | - | Unique email address |
| Phone | VARCHAR(20) | Yes | NULL | Contact phone number |
| Document | VARCHAR(20) | Yes | NULL | CPF or CNPJ |
| UserType | ENUM/VARCHAR | No | SelfRegistered | User type (SelfRegistered, Internal) |
| Role | ENUM/VARCHAR | No | Customer | User role (Customer, Admin, Manager) |
| IsActive | BOOLEAN | No | true | Account active status |
| EmailVerified | BOOLEAN | No | false | Email verification status |
| CreatedAt | TIMESTAMP | No | UtcNow | Creation timestamp |
| UpdatedAt | TIMESTAMP | Yes | NULL | Last update timestamp |
| CreatedByUserId | GUID | Yes | NULL | ID of user who created this record |

**Constraints**:
- Primary Key: Id
- Unique: Email
- Index: Email, Document, IsActive, (UserType, IsActive)

**Enums**:
- UserType: SelfRegistered, Internal
- Role: Customer, Admin, Manager, Sales

---

### Companies

Stores company account information with Brazilian tax data.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | GUID | No | NewGuid() | Primary key |
| Cnpj | VARCHAR(20) | No | - | Brazilian tax ID (unique) |
| CorporateName | VARCHAR(300) | No | - | Razão Social (legal name) |
| TradeName | VARCHAR(300) | Yes | NULL | Nome Fantasia (trade name) |
| StateRegistration | VARCHAR(50) | Yes | NULL | Inscrição Estadual |
| MunicipalRegistration | VARCHAR(50) | Yes | NULL | Inscrição Municipal |
| IsActive | BOOLEAN | No | true | Account active status |
| CreatedAt | TIMESTAMP | No | UtcNow | Creation timestamp |
| UpdatedAt | TIMESTAMP | Yes | NULL | Last update timestamp |
| CreatedByUserId | GUID | Yes | NULL | ID of user who created this record |

**Constraints**:
- Primary Key: Id
- Unique: Cnpj
- Index: Cnpj, IsActive

---

### CompanyUsers

Junction table for User-Company many-to-many relationship.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | GUID | No | NewGuid() | Primary key |
| CompanyId | GUID | No | - | Foreign key to Companies |
| UserId | GUID | No | - | Foreign key to Users |
| IsActive | BOOLEAN | No | true | Association active status |
| CreatedAt | TIMESTAMP | No | UtcNow | Association creation time |
| UpdatedAt | TIMESTAMP | Yes | NULL | Last update timestamp |

**Constraints**:
- Primary Key: Id
- Foreign Key: CompanyId → Companies.Id (CASCADE)
- Foreign Key: UserId → Users.Id (CASCADE)
- Unique: (CompanyId, UserId)
- Index: UserId, IsActive

---

### Addresses

Stores addresses for both users and companies.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | GUID | No | NewGuid() | Primary key |
| Type | ENUM/VARCHAR | No | Both | Address type |
| PostalCode | VARCHAR(10) | No | - | CEP (Brazilian postal code) |
| Street | VARCHAR(200) | No | - | Street name |
| Number | VARCHAR(20) | No | - | Street number |
| Complement | VARCHAR(100) | Yes | NULL | Additional info (apt, suite) |
| Neighborhood | VARCHAR(100) | No | - | Bairro |
| City | VARCHAR(100) | No | - | City name |
| State | VARCHAR(2) | No | - | State code (e.g., SP, RJ) |
| IsDefault | BOOLEAN | No | false | Default address flag |
| IsActive | BOOLEAN | No | true | Address active status |
| CreatedAt | TIMESTAMP | No | UtcNow | Creation timestamp |
| UpdatedAt | TIMESTAMP | Yes | NULL | Last update timestamp |
| UserId | GUID | Yes | NULL | Foreign key to Users (nullable) |
| CompanyId | GUID | Yes | NULL | Foreign key to Companies (nullable) |

**Constraints**:
- Primary Key: Id
- Foreign Key: UserId → Users.Id (CASCADE, nullable)
- Foreign Key: CompanyId → Companies.Id (CASCADE, nullable)
- Index: UserId, CompanyId, (IsActive, IsDefault)
- Check: Either UserId OR CompanyId must be set (not both)

**Enums**:
- Type: Billing, Shipping, Both, Commercial

---

### CommercialConditions

Stores commercial conditions (collections of rules).

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | GUID | No | NewGuid() | Primary key |
| Name | VARCHAR(200) | No | - | Condition name |
| Description | VARCHAR(1000) | Yes | NULL | Description |
| ValidFrom | TIMESTAMP | Yes | NULL | Validity start date |
| ValidUntil | TIMESTAMP | Yes | NULL | Validity end date |
| Priority | INTEGER | No | 0 | Priority (higher = more important) |
| IsActive | BOOLEAN | No | true | Condition active status |
| CreatedAt | TIMESTAMP | No | UtcNow | Creation timestamp |
| UpdatedAt | TIMESTAMP | Yes | NULL | Last update timestamp |
| CreatedByUserId | GUID | Yes | NULL | ID of user who created this record |

**Constraints**:
- Primary Key: Id
- Index: IsActive, (ValidFrom, ValidUntil), Priority

---

### ConditionRules

Stores individual rules within commercial conditions.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | GUID | No | NewGuid() | Primary key |
| CommercialConditionId | GUID | No | - | Foreign key to CommercialConditions |
| RuleType | ENUM/VARCHAR | No | - | Rule type (Visibility, Discount) |
| Expression | VARCHAR(2000) | No | - | Expression to evaluate |
| DiscountType | ENUM/VARCHAR | Yes | NULL | Discount type (if applicable) |
| DiscountValue | DECIMAL(10,2) | Yes | NULL | Discount value (if applicable) |
| Description | VARCHAR(500) | Yes | NULL | Rule description |
| Priority | INTEGER | No | 0 | Rule priority |
| IsActive | BOOLEAN | No | true | Rule active status |
| CreatedAt | TIMESTAMP | No | UtcNow | Creation timestamp |
| UpdatedAt | TIMESTAMP | Yes | NULL | Last update timestamp |

**Constraints**:
- Primary Key: Id
- Foreign Key: CommercialConditionId → CommercialConditions.Id (CASCADE)
- Index: (CommercialConditionId, RuleType), IsActive, Priority

**Enums**:
- RuleType: Visibility, Discount
- DiscountType: Percentage, Fixed

---

### CompanyCommercialConditions

Junction table for Company-CommercialCondition many-to-many relationship.

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | GUID | No | NewGuid() | Primary key |
| CompanyId | GUID | No | - | Foreign key to Companies |
| CommercialConditionId | GUID | No | - | Foreign key to CommercialConditions |
| IsActive | BOOLEAN | No | true | Assignment active status |
| CreatedAt | TIMESTAMP | No | UtcNow | Assignment creation time |
| UpdatedAt | TIMESTAMP | Yes | NULL | Last update timestamp |

**Constraints**:
- Primary Key: Id
- Foreign Key: CompanyId → Companies.Id (CASCADE)
- Foreign Key: CommercialConditionId → CommercialConditions.Id (CASCADE)
- Unique: (CompanyId, CommercialConditionId)
- Index: CommercialConditionId, IsActive

---

### AuditLogs

Stores audit trail for important operations (planned for future implementation).

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | GUID | No | NewGuid() | Primary key |
| EntityType | VARCHAR(100) | No | - | Type of entity modified |
| EntityId | GUID | No | - | ID of entity modified |
| Action | VARCHAR(50) | No | - | Action performed (Create, Update, Delete) |
| Changes | TEXT | Yes | NULL | JSON of changes made |
| PerformedAt | TIMESTAMP | No | UtcNow | When action was performed |
| PerformedByUserId | GUID | Yes | NULL | Who performed the action |

**Constraints**:
- Primary Key: Id
- Index: (EntityType, EntityId), PerformedAt, PerformedByUserId

## Indexes

Indexes are crucial for query performance. Here's a breakdown of all indexes in the database:

### Users Table

```sql
-- Unique index on email for fast lookups and uniqueness
CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);

-- Index on document for CPF/CNPJ lookups
CREATE INDEX IX_Users_Document ON Users (Document);

-- Index on IsActive for filtering active users
CREATE INDEX IX_Users_IsActive ON Users (IsActive);

-- Composite index for filtering by user type and status
CREATE INDEX IX_Users_UserType_IsActive ON Users (UserType, IsActive);
```

### Companies Table

```sql
-- Unique index on CNPJ for fast lookups and uniqueness
CREATE UNIQUE INDEX IX_Companies_Cnpj ON Companies (Cnpj);

-- Index on IsActive for filtering active companies
CREATE INDEX IX_Companies_IsActive ON Companies (IsActive);
```

### CompanyUsers Table

```sql
-- Unique composite index to prevent duplicate associations
CREATE UNIQUE INDEX IX_CompanyUsers_CompanyId_UserId ON CompanyUsers (CompanyId, UserId);

-- Index on UserId for reverse lookups (finding companies for a user)
CREATE INDEX IX_CompanyUsers_UserId ON CompanyUsers (UserId);

-- Index on IsActive for filtering active associations
CREATE INDEX IX_CompanyUsers_IsActive ON CompanyUsers (IsActive);
```

### Addresses Table

```sql
-- Index on UserId for finding user addresses
CREATE INDEX IX_Addresses_UserId ON Addresses (UserId);

-- Index on CompanyId for finding company addresses
CREATE INDEX IX_Addresses_CompanyId ON Addresses (CompanyId);

-- Composite index for filtering default active addresses
CREATE INDEX IX_Addresses_IsActive_IsDefault ON Addresses (IsActive, IsDefault);
```

### CommercialConditions Table

```sql
-- Index on IsActive for filtering active conditions
CREATE INDEX IX_CommercialConditions_IsActive ON CommercialConditions (IsActive);

-- Composite index for validity date queries
CREATE INDEX IX_CommercialConditions_ValidFrom_ValidUntil
ON CommercialConditions (ValidFrom, ValidUntil);

-- Index on Priority for ordering
CREATE INDEX IX_CommercialConditions_Priority ON CommercialConditions (Priority);
```

### ConditionRules Table

```sql
-- Composite index for finding rules by condition and type
CREATE INDEX IX_ConditionRules_CommercialConditionId_RuleType
ON ConditionRules (CommercialConditionId, RuleType);

-- Index on IsActive for filtering active rules
CREATE INDEX IX_ConditionRules_IsActive ON ConditionRules (IsActive);

-- Index on Priority for ordering
CREATE INDEX IX_ConditionRules_Priority ON ConditionRules (Priority);
```

### CompanyCommercialConditions Table

```sql
-- Unique composite index to prevent duplicate assignments
CREATE UNIQUE INDEX IX_CompanyCommercialConditions_CompanyId_CommercialConditionId
ON CompanyCommercialConditions (CompanyId, CommercialConditionId);

-- Index on CommercialConditionId for reverse lookups
CREATE INDEX IX_CompanyCommercialConditions_CommercialConditionId
ON CompanyCommercialConditions (CommercialConditionId);

-- Index on IsActive for filtering active assignments
CREATE INDEX IX_CompanyCommercialConditions_IsActive
ON CompanyCommercialConditions (IsActive);
```

### AuditLogs Table

```sql
-- Composite index for finding audit logs by entity
CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs (EntityType, EntityId);

-- Index on PerformedAt for time-based queries
CREATE INDEX IX_AuditLogs_PerformedAt ON AuditLogs (PerformedAt);

-- Index on PerformedByUserId for finding actions by user
CREATE INDEX IX_AuditLogs_PerformedByUserId ON AuditLogs (PerformedByUserId);
```

## Migrations

The database uses Entity Framework Core Code-First migrations for schema management.

### Migration Commands

#### Create a New Migration

```bash
cd src/UserManagementAPI
dotnet ef migrations add MigrationName
```

This creates migration files in the `Migrations` folder.

#### Apply Migrations to Database

```bash
# Apply all pending migrations
dotnet ef database update

# Apply migrations up to a specific migration
dotnet ef database update MigrationName

# Rollback to previous migration
dotnet ef database update PreviousMigrationName
```

#### Remove Last Migration

```bash
# Only works if migration hasn't been applied
dotnet ef migrations remove
```

#### Generate SQL Script

```bash
# Generate SQL for all migrations
dotnet ef migrations script

# Generate SQL from one migration to another
dotnet ef migrations script FromMigration ToMigration

# Generate SQL for production (idempotent)
dotnet ef migrations script --idempotent
```

### Migration Workflow

#### Development

1. Make changes to entity classes
2. Create migration: `dotnet ef migrations add DescriptiveName`
3. Review generated migration code
4. Apply migration: `dotnet ef database update`
5. Test changes locally

#### Production

Migrations are automatically applied on application startup in production:

```csharp
// From Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
    if (isProduction)
    {
        db.Database.Migrate(); // Applies pending migrations
    }
}
```

### Migration Best Practices

1. **Always review generated migrations** before applying
2. **Test migrations on a copy of production data** before deploying
3. **Create migrations with descriptive names** (e.g., `AddUserEmailVerification`)
4. **Never modify applied migrations** - create new ones instead
5. **Keep migrations focused** - one logical change per migration
6. **Include data migrations** when renaming/restructuring

### Example Migration

```csharp
public partial class AddCommercialConditions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CommercialConditions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)",
                    maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(1000)",
                    maxLength: 1000, nullable: true),
                ValidFrom = table.Column<DateTime>(type: "timestamp with time zone",
                    nullable: true),
                ValidUntil = table.Column<DateTime>(type: "timestamp with time zone",
                    nullable: true),
                Priority = table.Column<int>(type: "integer", nullable: false,
                    defaultValue: 0),
                IsActive = table.Column<bool>(type: "boolean", nullable: false,
                    defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone",
                    nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone",
                    nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommercialConditions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CommercialConditions_IsActive",
            table: "CommercialConditions",
            column: "IsActive");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CommercialConditions");
    }
}
```

## Seed Data

Currently, the database does not include automatic seed data. In development mode, the database is created empty using `EnsureCreated()`.

### Adding Seed Data (Future)

To add seed data, create a migration with data seeding:

```csharp
public partial class SeedInitialData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Seed default admin user
        migrationBuilder.InsertData(
            table: "Users",
            columns: new[] { "Id", "Name", "Email", "UserType", "Role",
                "IsActive", "EmailVerified", "CreatedAt" },
            values: new object[] {
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                "System Administrator",
                "admin@fgbrindes.com",
                "Internal",
                "Admin",
                true,
                true,
                DateTime.UtcNow
            });
    }
}
```

## Query Examples

### Common Queries

#### Find Active Users with Companies

```csharp
var usersWithCompanies = await _context.Users
    .Include(u => u.CompanyAssociations)
        .ThenInclude(ca => ca.Company)
    .Where(u => u.IsActive && !u.IsDeleted)
    .ToListAsync();
```

#### Find All Commercial Conditions for a User

```csharp
var conditions = await _context.Users
    .Where(u => u.Id == userId)
    .SelectMany(u => u.CompanyAssociations
        .Where(ca => ca.IsActive)
        .SelectMany(ca => ca.Company.CommercialConditions
            .Where(cc => cc.IsActive && cc.CommercialCondition.IsActive)
            .Select(cc => cc.CommercialCondition)))
    .Include(c => c.Rules.Where(r => r.IsActive))
    .Distinct()
    .OrderByDescending(c => c.Priority)
    .ToListAsync();
```

#### Find Discount Rules for a Company

```csharp
var discountRules = await _context.ConditionRules
    .Where(cr => cr.RuleType == RuleType.Discount
        && cr.IsActive
        && cr.CommercialCondition.IsActive
        && cr.CommercialCondition.Companies.Any(
            cc => cc.CompanyId == companyId && cc.IsActive))
    .OrderByDescending(cr => cr.Priority)
    .ToListAsync();
```

#### Get User with All Related Data

```csharp
var user = await _context.Users
    .Include(u => u.Addresses.Where(a => a.IsActive))
    .Include(u => u.CompanyAssociations.Where(ca => ca.IsActive))
        .ThenInclude(ca => ca.Company)
    .FirstOrDefaultAsync(u => u.Id == userId);
```

### Performance Optimization

#### Use AsNoTracking for Read-Only Queries

```csharp
var users = await _context.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .ToListAsync();
```

#### Use Pagination

```csharp
var users = await _context.Users
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

#### Project to DTOs

```csharp
var userSummaries = await _context.Users
    .Where(u => u.IsActive)
    .Select(u => new UserSummaryDTO
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        UserType = u.UserType.ToString(),
        Role = u.Role.ToString(),
        IsActive = u.IsActive
    })
    .ToListAsync();
```

## Maintenance

### Monitoring

#### Database Size

```sql
-- PostgreSQL: Check database size
SELECT pg_size_pretty(pg_database_size('usermanagement'));

-- PostgreSQL: Check table sizes
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

#### Index Usage

```sql
-- PostgreSQL: Check index usage
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes
ORDER BY idx_scan DESC;
```

### Backup and Restore

#### Backup Database (PostgreSQL)

```bash
# Full database backup
pg_dump -h hostname -U username -d usermanagement -F c -f backup.dump

# Schema only
pg_dump -h hostname -U username -d usermanagement -s -f schema.sql

# Data only
pg_dump -h hostname -U username -d usermanagement -a -f data.sql
```

#### Restore Database (PostgreSQL)

```bash
# Restore from custom format dump
pg_restore -h hostname -U username -d usermanagement backup.dump

# Restore from SQL file
psql -h hostname -U username -d usermanagement -f backup.sql
```

### Cleanup Operations

#### Soft Delete Cleanup (Not Implemented Yet)

When soft deletes are implemented, periodic cleanup of old deleted records:

```sql
-- Example cleanup of records deleted more than 1 year ago
DELETE FROM Users WHERE IsDeleted = true AND UpdatedAt < NOW() - INTERVAL '1 year';
```

### Performance Tuning

#### Analyze Tables (PostgreSQL)

```sql
-- Update statistics for query planner
ANALYZE Users;
ANALYZE Companies;
ANALYZE CommercialConditions;

-- Or analyze all tables
ANALYZE;
```

#### Reindex (PostgreSQL)

```sql
-- Rebuild all indexes for a table
REINDEX TABLE Users;

-- Rebuild specific index
REINDEX INDEX IX_Users_Email;

-- Rebuild all indexes in database
REINDEX DATABASE usermanagement;
```

### Connection String Examples

#### PostgreSQL (Production)

```
Host=your-db-host.rds.amazonaws.com;Port=5432;Database=usermanagement;Username=dbuser;Password=dbpassword;SSL Mode=Require
```

#### SQLite (Development)

```
Data Source=usermanagement.db
```

## Troubleshooting

### Common Issues

#### Migration Pending

**Error**: "Pending model changes"

**Solution**: Create and apply a new migration
```bash
dotnet ef migrations add FixModelChanges
dotnet ef database update
```

#### Connection Issues

**Error**: Cannot connect to database

**Solution**:
1. Verify connection string
2. Check network connectivity
3. Verify PostgreSQL is running
4. Check firewall rules

#### Slow Queries

**Solution**:
1. Review query execution plans
2. Add missing indexes
3. Use AsNoTracking for read-only queries
4. Implement pagination
5. Optimize Include statements

## Additional Resources

- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Database Design Best Practices](https://docs.microsoft.com/azure/architecture/best-practices/data-partitioning)

---

For database-related questions or issues, contact: github@fgbrind.es
