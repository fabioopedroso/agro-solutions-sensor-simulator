using Application.DTOs;
using Application.Services;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SensorSimulatorSettings>(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ConnectionString")));

        services.AddScoped<IFieldDataAccess, FieldDataAccess>();
        services.AddScoped<IFieldService, FieldService>();

        services.AddHostedService<SoilHumidityWorker>();
        services.AddHostedService<TemperatureWorker>();
        services.AddHostedService<RainfallWorker>();

        return services;
    }
}
