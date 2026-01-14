using Ats.Integration;
using Ats.Integration.Messaging;
using AuthorizationService.Data;
using AuthorizationService.Services;
using Ats.ServiceDiscovery.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ==========================
// CONFIGURATION FROM ENV
// ==========================

var config = builder.Configuration;

// PostgreSQL
var postgresHost = config["POSTGRES_HOST"] ?? "localhost";
var postgresPort = config["POSTGRES_PORT"] ?? "5432";
var postgresDb = config["POSTGRES_DB"] ?? "ats_auth";
var postgresUser = config["POSTGRES_USER"] ?? "ats";
var postgresPass = config["POSTGRES_PASSWORD"] ?? "ats_pass";

var pgConnString = 
    $"Host={postgresHost};Port={postgresPort};Database={postgresDb};Username={postgresUser};Password={postgresPass}";

// Keycloak
builder.Services.Configure<KeycloakOptions>(options =>
{
    options.BaseUrl = config["KEYCLOAK_URL"] ?? "http://localhost:8080";
    options.Realm = config["KEYCLOAK_REALM"] ?? "ats";
    options.AdminClientId = config["KEYCLOAK_ADMIN_CLIENT_ID"] ?? "admin-cli";
    options.AdminClientSecret = config["KEYCLOAK_ADMIN_CLIENT_SECRET"];
    options.AdminUsername = config["KEYCLOAK_ADMIN_USERNAME"] ?? "admin";
    options.AdminPassword = config["KEYCLOAK_ADMIN_PASSWORD"] ?? "admin";
});

// ==========================
// DATABASE
// ==========================

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(pgConnString));

// ==========================
// SERVICE DISCOVERY (optional)
// ==========================

var serviceDiscoveryEnabled = config.GetValue("SERVICE_DISCOVERY_ENABLED", true);

if (serviceDiscoveryEnabled)
{
    builder.Services.AddServiceDiscovery(config);
}

// ==========================
// INTEGRATION (RabbitMQ, etc.)
// ==========================

builder.Services.AddAtsIntegration(config);

builder.Services.AddHttpClient<KeycloakAdminClient>();
builder.Services.AddScoped<KeycloakAdminClient>();


// ==========================
// HEALTH CHECKS
// ==========================

builder.Services.AddHealthChecks()
    .AddNpgSql(pgConnString, name: "postgres");

// ==========================
// CORS CONFIGURATION
// ==========================

var corsEnabled = config.GetValue("Cors:Enabled", true);
var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
var isDevelopment = config.GetValue("ASPNETCORE_ENVIRONMENT", "Production") == "Development";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsEnabled)
        {
            if (allowedOrigins != null && allowedOrigins.Length > 0)
            {
                // Use configured origins
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }
            else if (isDevelopment)
            {
                // In development, allow all origins for easier testing (Kubernetes, localhost, etc.)
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }
            else
            {
                // Production: default to localhost origins
                policy.WithOrigins(
                        "http://localhost:3000",
                        "http://localhost:5173",
                        "http://127.0.0.1:3000"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }
        }
    });
});

// ==========================
// SWAGGER + CONTROLLERS
// ==========================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ===========================
// BUILD APP
// ===========================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

// Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// ===========================
// DATABASE MIGRATION
// ===========================

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Database migration error: " + ex.Message);
    }
}

// ===========================
// MIDDLEWARE
// ===========================

// CORS must be before other middleware
app.UseCors();

app.UseAuthorization();
app.MapControllers();

app.Run();