using Ats.CandidateService.Users;
using Ats.Integration;
using Ats.Integration.Messaging;
using Ats.Integration.Users;
using VacancyService.Config;
using VacancyService.Data;
using Ats.ServiceDiscovery.Client;
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

builder.Services.AddControllers();
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

// Health checks
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();