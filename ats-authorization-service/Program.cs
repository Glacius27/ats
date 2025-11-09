
using Ats.Integration;
using Ats.Integration.Messaging;
using AuthorizationService.Data;
using AuthorizationService.Services;
using Ats.ServiceDiscovery.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));



builder.Services.AddServiceDiscovery(builder.Configuration);
builder.Services.AddAtsIntegration(builder.Configuration);
builder.Services.Configure<KeycloakOptions>(
    builder.Configuration.GetSection("Keycloak"));
builder.Services.AddHttpClient<KeycloakAdminClient>();
builder.Services.AddScoped<KeycloakAdminClient>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//await app.UseAtsIntegrationAsync();
app.UseAuthorization();
app.MapControllers();

app.Run();