using Ats.Integration;
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


builder.Services.AddAtsIntegration(builder.Configuration);
builder.Services.AddUserEvents();
builder.Services.AddUserSnapshot(); 


builder.Services.AddSingleton<UserCache>();
builder.Services.AddSingleton<IUserCacheHandler, CandidateUserCache>();


builder.Services.AddServiceDiscovery(builder.Configuration);


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


await app.UseAtsIntegrationAsync();


using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.RegisterUserEventSubscriptions();
}


using (var scope = app.Services.CreateScope())
{
    var loader = scope.ServiceProvider.GetRequiredService<UserSnapshotLoader>();
    await loader.LoadSnapshotAsync(); 
}


app.UseHttpsRedirection();
app.MapControllers();

app.Run();