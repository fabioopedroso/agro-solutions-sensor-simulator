using Application.DTOs;
using Application.Services;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configura as settings fortemente tipadas
builder.Services.Configure<SensorSimulatorSettings>(builder.Configuration);

// Configura DbContext para acesso aos talhões
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    options.UseNpgsql(configuration.GetConnectionString("ConnectionString"));
});

// Registra o HttpClient para o SensorDataService
builder.Services.AddHttpClient<SensorDataService>();

// Registra o HttpClient para o OpenMeteoService
builder.Services.AddHttpClient<IOpenMeteoService, OpenMeteoService>();

// Registra o SensorDataService como Scoped
builder.Services.AddScoped<SensorDataService>();

// Registra serviços de aplicação
builder.Services.AddScoped<IFieldService, FieldService>();

// Adiciona a camada de infraestrutura (workers e repositórios)
builder.Services.AddInfrastructure();

// Adiciona controllers e Swagger (opcional, para monitoramento)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "AgroSolutions Sensor Simulator",
        Version = "v1",
        Description = "Simulador de sensores IoT para o sistema AgroSolutions"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Endpoint de health check simples
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    service = "sensor-simulator"
}))
.WithName("HealthCheck")
.WithTags("Health");

app.Run();
