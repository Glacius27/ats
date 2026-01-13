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
// SWAGGER + CONTROLLERS
// ==========================

builder.Services.AddControllers();
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

app.UseAuthorization();
app.MapControllers();

app.Run();