using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using ats_recruitment_service.Data;
using Ats.CandidateService.Users;
using Ats.Integration;
using Ats.Integration.Messaging;
using Ats.Integration.Users;
using Ats.ServiceDiscovery.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Health checks
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecruitmentContext>();
    db.Database.Migrate();
}
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecruitmentContext>();
    db.Database.Migrate();
}

app.Run();