using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using ats_recruitment_service.Data;
using Ats.CandidateService.Users;
using Ats.Integration;
using Ats.Integration.Messaging;
using Ats.Integration.Users;
using Ats.ServiceDiscovery.Client;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL
builder.Services.AddDbContext<RecruitmentContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));


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

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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