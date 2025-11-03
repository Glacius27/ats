using ats_service_discovery.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using ServiceDiscovery.Models;
using ServiceDiscovery.Services;

var builder = WebApplication.CreateBuilder(args);

// Redis config
var redisSection = builder.Configuration.GetSection("Redis");
var redisHost = redisSection.GetValue<string>("Host")!;
var redisPort = redisSection.GetValue<int>("Port");
var redisPassword = redisSection.GetValue<string>("Password");
var redisDb = redisSection.GetValue<int>("DefaultDb");

var redisConnStr = string.IsNullOrEmpty(redisPassword)
    ? $"{redisHost}:{redisPort},defaultDatabase={redisDb}"
    : $"{redisHost}:{redisPort},password={redisPassword},defaultDatabase={redisDb}";

// DI
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnStr));
builder.Services.AddSingleton<RedisRegistry>();

// ✅ Регистрируем опции RabbitMQ и сам паблишер правильно (через IOptions)
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoints
app.MapPost("/register", async (ServiceInstance instance, RedisRegistry registry, RabbitMqPublisher publisher) =>
{
    try
    {
        var isNew = await registry.RegisterAsync(instance);

        if (isNew)
        {
            var evt = new ServiceEvent
            {
                Event = "register",
                Service = instance.Name,
                Host = instance.Host,
                Port = instance.Port
            };

            await publisher.PublishAsync(evt);
        }

        return Results.Ok(new
        {
            status = isNew ? "newly_registered" : "heartbeat_refreshed",
            key = $"service:{instance.Name}:{instance.Host}:{instance.Port}",
            expiresIn = 60
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});


app.MapGet("/services", async (RedisRegistry registry) =>
{
    var services = await registry.GetAllAsync();
    return Results.Ok(services);
});

app.MapGet("/services/{name}", async (string name, RedisRegistry registry) =>
{
    var services = await registry.GetByNameAsync(name);
    return services.Any() ? Results.Ok(services) : Results.NotFound();
});

app.Run();
