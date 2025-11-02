using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using ServiceDiscovery.Models;
using ServiceDiscovery.Services;

var builder = WebApplication.CreateBuilder(args);

// Загружаем параметры Redis из appsettings.json
var redisConfig = builder.Configuration.GetSection("Redis");
var host = redisConfig.GetValue<string>("Host");
var port = redisConfig.GetValue<int>("Port");
var password = redisConfig.GetValue<string>("Password");
var db = redisConfig.GetValue<int>("DefaultDb");

// Формируем строку подключения
var redisConnectionString = string.IsNullOrEmpty(password)
    ? $"{host}:{port},defaultDatabase={db}"
    : $"{host}:{port},password={password},defaultDatabase={db}";

// Регистрируем Redis и наш Registry
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<RedisRegistry>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Эндпоинты
app.MapPost("/register", async (ServiceInstance instance, RedisRegistry registry) =>
{
    await registry.RegisterAsync(instance);
    return Results.Ok(new { status = "registered", expiresIn = 60 });
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