using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using ats_recruitment_service.Data;
using Ats.CandidateService.Users;
using Ats.Integration;
using Ats.Integration.Messaging;
using Ats.Integration.Users;
using Ats.ServiceDiscovery.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ==========================
// CONFIGURATION FROM ENV
// ==========================

var config = builder.Configuration;

// PostgreSQL
var postgresHost = config["POSTGRES_HOST"] ?? "localhost";
var postgresPort = config["POSTGRES_PORT"] ?? "5432";
var postgresDb = config["POSTGRES_DB"] ?? "ats_recruitment";
var postgresUser = config["POSTGRES_USER"] ?? "ats";
var postgresPass = config["POSTGRES_PASSWORD"] ?? "ats_pass";

var pgConnString = 
    $"Host={postgresHost};Port={postgresPort};Database={postgresDb};Username={postgresUser};Password={postgresPass}";

// ==========================
// DATABASE
// ==========================

builder.Services.AddDbContext<RecruitmentContext>(options =>
    options.UseNpgsql(pgConnString));

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(pgConnString, name: "postgres");

// CORS configuration
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

// Service Discovery (optional)
var serviceDiscoveryEnabled = config.GetValue("SERVICE_DISCOVERY_ENABLED", false);
if (serviceDiscoveryEnabled)
{
    builder.Services.AddServiceDiscovery(builder.Configuration);
}

builder.Services.AddAtsIntegration(builder.Configuration);
builder.Services.AddSingleton<UserCache>();
builder.Services.AddSingleton<UserCacheHandler>();
builder.Services.AddSingleton<IUserCacheHandler>(sp => sp.GetRequiredService<UserCacheHandler>());

// Resolve AuthService URL
if (serviceDiscoveryEnabled)
{
    using (var tempProvider = builder.Services.BuildServiceProvider())
    {
        var sd = tempProvider.GetRequiredService<IServiceDiscoveryClient>();
        var authInstance = await sd.GetSingleServiceAsync("ats-authorization-service");

        if (authInstance is not null)
        {
            var authUrl = $"http://{authInstance.Host}:{authInstance.Port}";
            builder.Configuration["AuthService:BaseUrl"] = authUrl;
            Console.WriteLine($"[SD] AuthService resolved via SD → {authUrl}");
        }
        else
        {
            Console.WriteLine("[SD] AuthService not found in SD registry, will use config value.");
        }
    }
}
else
{
    // Use Kubernetes DNS or config value
    var authUrl = config["AuthService__BaseUrl"] ?? config["AuthService:BaseUrl"] ?? "http://ats-authorization-service:8080";
    builder.Configuration["AuthService:BaseUrl"] = authUrl;
    Console.WriteLine($"[K8s] AuthService URL: {authUrl}");
}

builder.Services.AddUserSnapshotHostedLoader<UserCacheHandler>(); 
builder.Services.AddUserEvents();       

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Database migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecruitmentContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health checks
app.MapHealthChecks("/health");

// Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// CORS must be before other middleware (as in authorization-service)
app.UseCors();

app.UseAuthorization();
app.MapControllers();

app.Run();