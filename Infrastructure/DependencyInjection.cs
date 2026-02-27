using Application.DTOs;
using Application.Services;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SensorSimulatorSettings>(configuration);

        var connectionString = configuration.GetConnectionString("ConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'ConnectionString' não encontrada ou vazia em ConnectionStrings no appsettings.json.");

        services.AddScoped<IFieldDataAccess>(sp =>
            new FieldDataAccess(connectionString, sp.GetRequiredService<ILogger<FieldDataAccess>>()));

        services.AddScoped<IFieldService, FieldService>();

        services.AddHostedService<SoilHumidityWorker>();
        services.AddHostedService<TemperatureWorker>();
        services.AddHostedService<RainfallWorker>();

        return services;
    }
}
