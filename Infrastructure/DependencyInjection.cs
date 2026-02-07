using Infrastructure.Helpers;
using Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<GradualValueGenerator>();

        services.AddHostedService<SoilHumidityWorker>();
        services.AddHostedService<TemperatureWorker>();
        services.AddHostedService<RainfallWorker>();

        return services;
    }
}
