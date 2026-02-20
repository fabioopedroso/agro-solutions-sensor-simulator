using Application.Services;
using Infrastructure.Persistence;
using Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Data Access
        services.AddScoped<IFieldDataAccess, FieldDataAccess>();

        // Workers
        services.AddHostedService<SoilHumidityWorker>();
        services.AddHostedService<TemperatureWorker>();
        services.AddHostedService<RainfallWorker>();

        return services;
    }
}
