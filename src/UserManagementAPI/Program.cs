using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using UserManagementAPI.Data;
using UserManagementAPI.Models.Entities;
using UserManagementAPI.Repositories;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ====================================
// 1. DATABASE CONFIGURATION
// ====================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var isProduction = builder.Environment.IsProduction();

builder.Services.AddDbContext<UserManagementDbContext>(options =>
{
    if (isProduction)
    {
        // Priority: DATABASE_URL → ConnectionStrings__PostgresConnection → appsettings
        var postgresConnection = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgresConnection")
            ?? builder.Configuration.GetConnectionString("PostgresConnection")
            ?? connectionString;

        if (string.IsNullOrEmpty(postgresConnection))
            throw new InvalidOperationException("PostgreSQL connection string not found");

        options.UseNpgsql(postgresConnection, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });
    }
    else
    {
        // Development: Use SQLite for easier local development
        var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection")
            ?? "Data Source=usermanagement.db";
        options.UseSqlite(sqliteConnection);
    }
});

// ====================================
// 2. CORS CONFIGURATION
// ====================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")?.Split(',')
            ?? builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "*" };

        if (corsOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders("X-Total-Count", "X-Page", "X-Page-Size");
        }
    });
});

// ====================================
// 3. CONTROLLERS & JSON OPTIONS
// ====================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ====================================
// 4. SWAGGER/OPENAPI
// ====================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Management API",
        Version = "v1",
        Description = @"API para gerenciamento de usuários, empresas e condições comerciais para a plataforma de e-commerce FG Brindes.

## Funcionalidades Principais

- **Gestão de Usuários**: Cadastro, atualização e controle de usuários (auto-cadastrados ou internos)
- **Gestão de Empresas**: Gerenciamento de empresas e associação com usuários
- **Condições Comerciais**: Regras de desconto e visibilidade de catálogo baseadas em expressões
- **APIs de Integração**: Endpoints para consumo pelos serviços de Catálogo, Carrinho e Cotação

## Integrações

Esta API fornece dados para:
- **Catalog Service**: Regras de visibilidade de produtos
- **Cart & Quote Service**: Regras de desconto aplicáveis
- **Frontend**: Gestão completa de usuários e empresas",
        Contact = new OpenApiContact
        {
            Name = "FG Brindes",
            Email = "github@fgbrind.es"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Order by HTTP method
    c.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
});

// ====================================
// 5. HEALTH CHECKS
// ====================================
builder.Services.AddHealthChecks()
    .AddDbContextCheck<UserManagementDbContext>();

// ====================================
// 6. DEPENDENCY INJECTION - Repositories
// ====================================
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ICommercialConditionRepository, CommercialConditionRepository>();
builder.Services.AddScoped<IConditionRuleRepository, ConditionRuleRepository>();

// ====================================
// 7. DEPENDENCY INJECTION - Services
// ====================================
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ICommercialConditionService, CommercialConditionService>();
builder.Services.AddScoped<IIntegrationService, IntegrationService>();

// ====================================
// 8. MONITORING
// ====================================
var appInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];

if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}

var app = builder.Build();

// ====================================
// 9. APPLY MIGRATIONS ON STARTUP
// ====================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (isProduction)
        {
            logger.LogInformation("Applying database migrations...");
            db.Database.Migrate();
            logger.LogInformation("✅ Database migrations applied successfully");
        }
        else
        {
            logger.LogInformation("Ensuring database is created (development mode)...");
            db.Database.EnsureCreated();
            logger.LogInformation("✅ Database ready");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error initializing database");
        throw;
    }
}

// ====================================
// 10. MIDDLEWARE PIPELINE
// ====================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
