using Ats.CandidateService.Users;
using Ats.Integration;
using Ats.Integration.Messaging;
using Ats.Integration.Users;
using VacancyService.Config;
using VacancyService.Data;
using Ats.ServiceDiscovery.Client;
var builder = WebApplication.CreateBuilder(args);

// MongoDB settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<VacancyRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddServiceDiscovery(builder.Configuration);
builder.Services.AddAtsIntegration(builder.Configuration);
builder.Services.AddSingleton<UserCache>();
builder.Services.AddSingleton<UserCacheHandler>();
builder.Services.AddSingleton<IUserCacheHandler>(sp => sp.GetRequiredService<UserCacheHandler>());

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

builder.Services.AddUserSnapshotHostedLoader<UserCacheHandler>(); 
builder.Services.AddUserEvents();       

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();