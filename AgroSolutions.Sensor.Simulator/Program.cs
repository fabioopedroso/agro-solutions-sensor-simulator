using Application.Services;
using Application.Services.Interfaces;
using Infrastructure;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["Observability:ServiceName"] ?? "AgroSolutions.Sensor.Simulator";
var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"];

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName)
    .AddTelemetrySdk()
    .AddEnvironmentVariableDetector();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.SetResourceBuilder(resourceBuilder);

    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
    {
        logging.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
        });
    }
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = context => !context.Request.Path.Value!.Contains("/health");
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.EnrichWithIDbCommand = (activity, command) =>
                {
                    activity.SetTag("db.statement", command.CommandText);
                };
            })
            .AddNpgsql();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
            });
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(resourceBuilder)
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("Npgsql");

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
            });
        }
    });

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpClient<ISensorDataService, SensorDataService>();
builder.Services.AddHttpClient<IOpenMeteoService, OpenMeteoService>();

builder.Services.AddHealthChecks();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgroSolutions Sensor Simulator",
        Version = "v1",
        Description = "Simulador de sensores IoT para o sistema AgroSolutions"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
