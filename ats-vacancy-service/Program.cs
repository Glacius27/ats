using Ats.CandidateService.Users;
using Ats.Integration;
using Ats.Integration.Messaging;
using Ats.Integration.Users;
using VacancyService.Config;
using VacancyService.Data;
using Ats.ServiceDiscovery.Client;
using Prometheus;
var builder = WebApplication.CreateBuilder(args);

// ==========================
// CONFIGURATION FROM ENV
// ==========================

var config = builder.Configuration;

// MongoDB settings
builder.Services.Configure<MongoDbSettings>(options =>
{
    options.ConnectionString = config["MongoDbSettings__ConnectionString"] 
        ?? config["MongoDbSettings:ConnectionString"] 
        ?? "mongodb://localhost:27017";
    options.DatabaseName = config["MongoDbSettings__DatabaseName"] 
        ?? config["MongoDbSettings:DatabaseName"] 
        ?? "ats_vacancy_db";
    options.VacanciesCollection = config["MongoDbSettings__VacanciesCollection"] 
        ?? config["MongoDbSettings:VacanciesCollection"] 
        ?? "Vacancies";
});

builder.Services.AddSingleton<VacancyRepository>();

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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health checks (simple check, no database dependency)
builder.Services.AddHealthChecks();

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

var app = builder.Build();

// CORS must be before other middleware
app.UseCors();

// Health checks
app.MapHealthChecks("/health");

// Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();