using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Models.Entities;
using UserManagementAPI.Models.Enums;

namespace UserManagementAPI.Data;

public class UserManagementDbContext : DbContext
{
    public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<CompanyUser> CompanyUsers { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<CommercialCondition> CommercialConditions { get; set; }
    public DbSet<CompanyCommercialCondition> CompanyCommercialConditions { get; set; }
    public DbSet<ConditionRule> ConditionRules { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ====================================
        // USER Configuration
        // ====================================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Document);
            entity.HasIndex(u => u.IsActive);
            entity.HasIndex(u => new { u.UserType, u.IsActive });

            entity.Property(u => u.UserType).HasConversion<string>();
            entity.Property(u => u.Role).HasConversion<string>();

            // Configure one-to-many with Addresses
            entity.HasMany(u => u.Addresses)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many with CompanyUsers
            entity.HasMany(u => u.CompanyAssociations)
                .WithOne(cu => cu.User)
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ====================================
        // COMPANY Configuration
        // ====================================
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(c => c.Cnpj).IsUnique();
            entity.HasIndex(c => c.IsActive);

            // Configure one-to-many with Addresses
            entity.HasMany(c => c.Addresses)
                .WithOne(a => a.Company)
                .HasForeignKey(a => a.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many with CompanyUsers
            entity.HasMany(c => c.CompanyUsers)
                .WithOne(cu => cu.Company)
                .HasForeignKey(cu => cu.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many with CompanyCommercialConditions
            entity.HasMany(c => c.CommercialConditions)
                .WithOne(ccc => ccc.Company)
                .HasForeignKey(ccc => ccc.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ====================================
        // COMPANYUSER Configuration
        // ====================================
        modelBuilder.Entity<CompanyUser>(entity =>
        {
            entity.HasIndex(cu => new { cu.CompanyId, cu.UserId }).IsUnique();
            entity.HasIndex(cu => cu.UserId);
            entity.HasIndex(cu => cu.IsActive);
        });

        // ====================================
        // ADDRESS Configuration
        // ====================================
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.CompanyId);
            entity.HasIndex(a => new { a.IsActive, a.IsDefault });

            entity.Property(a => a.Type).HasConversion<string>();
        });

        // ====================================
        // COMMERCIALCONDITION Configuration
        // ====================================
        modelBuilder.Entity<CommercialCondition>(entity =>
        {
            entity.HasIndex(cc => cc.IsActive);
            entity.HasIndex(cc => new { cc.ValidFrom, cc.ValidUntil });
            entity.HasIndex(cc => cc.Priority);

            // Configure one-to-many with CompanyCommercialConditions
            entity.HasMany(cc => cc.Companies)
                .WithOne(ccc => ccc.CommercialCondition)
                .HasForeignKey(ccc => ccc.CommercialConditionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many with ConditionRules
            entity.HasMany(cc => cc.Rules)
                .WithOne(cr => cr.CommercialCondition)
                .HasForeignKey(cr => cr.CommercialConditionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ====================================
        // COMPANYCOMMERCIALCONDITION Configuration
        // ====================================
        modelBuilder.Entity<CompanyCommercialCondition>(entity =>
        {
            entity.HasIndex(ccc => new { ccc.CompanyId, ccc.CommercialConditionId }).IsUnique();
            entity.HasIndex(ccc => ccc.CommercialConditionId);
            entity.HasIndex(ccc => ccc.IsActive);
        });

        // ====================================
        // CONDITIONRULE Configuration
        // ====================================
        modelBuilder.Entity<ConditionRule>(entity =>
        {
            entity.HasIndex(cr => new { cr.CommercialConditionId, cr.RuleType });
            entity.HasIndex(cr => cr.IsActive);
            entity.HasIndex(cr => cr.Priority);

            entity.Property(cr => cr.RuleType).HasConversion<string>();
            entity.Property(cr => cr.DiscountType).HasConversion<string>();
        });

        // ====================================
        // AUDITLOG Configuration
        // ====================================
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(al => new { al.EntityType, al.EntityId });
            entity.HasIndex(al => al.PerformedAt);
            entity.HasIndex(al => al.PerformedByUserId);
        });
    }
}
