using Ats.Messaging;
using Ats.Messaging.Extensions;
using Ats.Messaging.Options;
using Ats.Users.Extensions;
using Ats.Users.Services;
using Ats.ServiceDiscovery.Client;
using CandidateService.Data;
using Microsoft.EntityFrameworkCore;
using Minio;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<CandidateContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.Configure<MessagingOptions>(
    builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddMessaging();


builder.Services.AddUserIntegration();


builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var config = builder.Configuration.GetSection("MinioSettings");
    return new MinioClient()
        .WithEndpoint(config["Endpoint"])
        .WithCredentials(config["AccessKey"], config["SecretKey"])
        .Build();
});

builder.Services.AddServiceDiscovery(builder.Configuration);
builder.Services.AddUserIntegration();

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


await app.UseMessagingAsync();


using (var scope = app.Services.CreateScope())
{
    var loader = scope.ServiceProvider.GetRequiredService<UserSnapshotLoader>();
    await loader.LoadSnapshotAsync();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();