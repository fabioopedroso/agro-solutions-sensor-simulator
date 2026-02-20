using Application.DTOs;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Workers;

public class SoilHumidityWorker : BackgroundService
{
    private readonly ILogger<SoilHumidityWorker> _logger;
    private readonly SensorSimulatorSettings _settings;
    private readonly IServiceProvider _serviceProvider;

    public SoilHumidityWorker(
        ILogger<SoilHumidityWorker> logger,
        IOptions<SensorSimulatorSettings> settings,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SoilHumidityWorker iniciado");

        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var fieldService = scope.ServiceProvider.GetRequiredService<IFieldService>();
                var openMeteoService = scope.ServiceProvider.GetRequiredService<IOpenMeteoService>();
                var sensorService = scope.ServiceProvider.GetRequiredService<SensorDataService>();

                // Busca talhões ativos
                var fields = await fieldService.GetActiveFieldsAsync(stoppingToken);
                var fieldsList = fields.ToList();

                if (!fieldsList.Any())
                {
                    _logger.LogWarning("Nenhum talhão ativo encontrado. Aguardando próximo ciclo...");
                    await Task.Delay(TimeSpan.FromSeconds(_settings.Workers.IntervalSeconds), stoppingToken);
                    continue;
                }

                _logger.LogInformation("Processando {Count} talhões para umidade do solo", fieldsList.Count);

                // Processa cada talhão
                var tasks = fieldsList.Select(async field =>
                {
                    try
                    {
                        var weatherData = await openMeteoService.GetWeatherDataAsync(
                            field.Latitude,
                            field.Longitude,
                            stoppingToken);

                        if (weatherData == null || !weatherData.Hourly.SoilMoisture0To7cm.Any())
                        {
                            _logger.LogWarning(
                                "Não foi possível obter dados de umidade do solo para o talhão {FieldId}",
                                field.Id);
                            return;
                        }

                        // Extrai o valor mais recente (primeiro índice)
                        var soilMoistureValue = weatherData.Hourly.SoilMoisture0To7cm[0];

                        if (soilMoistureValue == null)
                        {
                            _logger.LogWarning(
                                "Valor de umidade do solo nulo para o talhão {FieldId}",
                                field.Id);
                            return;
                        }

                        // Converte de m³/m³ para porcentagem (multiplica por 100)
                        var valueInPercentage = soilMoistureValue.Value * 100;

                        var sensorData = new SensorDataRequestDto(
                            FieldId: field.Id,
                            SensorType: "SoilHumidity",
                            Value: valueInPercentage,
                            Timestamp: DateTime.UtcNow
                        );

                        await sensorService.SendSensorDataAsync(sensorData, stoppingToken);

                        _logger.LogDebug(
                            "Dados de umidade do solo enviados: FieldId={FieldId}, Value={Value}%",
                            field.Id, valueInPercentage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Erro ao processar talhão {FieldId} para umidade do solo",
                            field.Id);
                    }
                });

                await Task.WhenAll(tasks);

                // Atualiza cache de talhões para detectar novos
                await fieldService.RefreshFieldsCacheAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(_settings.Workers.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SoilHumidityWorker cancelado");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no SoilHumidityWorker");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("SoilHumidityWorker finalizado");
    }
}
