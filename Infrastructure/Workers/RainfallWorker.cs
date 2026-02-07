using Application.DTOs;
using Application.Models;
using Application.Services;
using Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Workers;

public class RainfallWorker : BackgroundService
{
    private readonly ILogger<RainfallWorker> _logger;
    private readonly SensorSimulatorSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly GradualValueGenerator _valueGenerator;
    private SensorSimulatorState? _state;

    public RainfallWorker(
        ILogger<RainfallWorker> logger,
        IOptions<SensorSimulatorSettings> settings,
        IServiceProvider serviceProvider,
        GradualValueGenerator valueGenerator)
    {
        _logger = logger;
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
        _valueGenerator = valueGenerator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RainfallWorker iniciado");

        var config = _settings.Simulation.Rainfall;
        _state = new SensorSimulatorState(config.InitialValue, config.Delta);

        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var value = _valueGenerator.GenerateNext(_state, config.MinValue, config.MaxValue);

                var fieldId = _settings.Simulation.FieldIds[Random.Shared.Next(_settings.Simulation.FieldIds.Length)];

                var sensorData = new SensorDataRequestDto(
                    FieldId: fieldId,
                    SensorType: "Rainfall",
                    Value: value,
                    Timestamp: DateTime.UtcNow
                );

                using var scope = _serviceProvider.CreateScope();
                var sensorService = scope.ServiceProvider.GetRequiredService<SensorDataService>();
                await sensorService.SendSensorDataAsync(sensorData, stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(_settings.Simulation.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RainfallWorker cancelado");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no RainfallWorker");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("RainfallWorker finalizado");
    }
}
