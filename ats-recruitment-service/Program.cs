using ats_recruitment_service.Data;
using Microsoft.EntityFrameworkCore;
using Ats.ServiceDiscovery.Client;

var builder = WebApplication.CreateBuilder(args);

// Подключаем DbContext
builder.Services.AddDbContext<RecruitmentContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Контроллеры
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddServiceDiscovery(builder.Configuration);
var app = builder.Build();

// Включаем Swagger только в Dev
// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();