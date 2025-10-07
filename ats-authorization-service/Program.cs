using AuthorizationService.Data;
using AuthorizationService.Messaging;
using AuthorizationService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ───────────────────────────────
// 📦 Controllers + Swagger
// ───────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));


builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<RabbitMqPublisher>();


builder.Services.Configure<KeycloakOptions>(
    builder.Configuration.GetSection("Keycloak"));


builder.Services.AddHttpClient<KeycloakAdminClient>();

builder.Services.AddSingleton(sp =>
{
    var opt = sp.GetRequiredService<IOptions<KeycloakOptions>>().Value;
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(KeycloakAdminClient));
    return new KeycloakAdminClient(http, opt);
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

Console.WriteLine("✅ Authorization Service started successfully.");
Console.WriteLine($"➡️  Environment: {app.Environment.EnvironmentName}");

app.Run();