using Ats.CandidateService.Users;
using Ats.Integration;
using Ats.Integration.Messaging;
using Ats.Integration.Users;
using Ats.ServiceDiscovery.Client;
using CandidateService.Data;
using CandidateService.Services;
using Microsoft.EntityFrameworkCore;
using Minio;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<CandidateContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));


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







builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var config = builder.Configuration.GetSection("MinioSettings");
    return new MinioClient()
        .WithEndpoint(config["Endpoint"])
        .WithCredentials(config["AccessKey"], config["SecretKey"])
        .Build();
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CandidateContext>();
    db.Database.Migrate();
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();