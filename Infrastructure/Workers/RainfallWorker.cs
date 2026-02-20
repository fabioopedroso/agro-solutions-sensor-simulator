using Application.DTOs;
using Application.Services;
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

    public RainfallWorker(
        ILogger<RainfallWorker> logger,
        IOptions<SensorSimulatorSettings> settings,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RainfallWorker iniciado");

        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);

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

                _logger.LogInformation("Processando {Count} talhões para precipitação", fieldsList.Count);

                // Processa cada talhão
                var tasks = fieldsList.Select(async field =>
                {
                    try
                    {
                        var weatherData = await openMeteoService.GetWeatherDataAsync(
                            field.Latitude,
                            field.Longitude,
                            stoppingToken);

                        if (weatherData == null || !weatherData.Hourly.Rain.Any())
                        {
                            _logger.LogWarning(
                                "Não foi possível obter dados de precipitação para o talhão {FieldId}",
                                field.Id);
                            return;
                        }

                        // Extrai o valor mais recente (primeiro índice)
                        var rainValue = weatherData.Hourly.Rain[0];

                        if (rainValue == null)
                        {
                            _logger.LogWarning(
                                "Valor de precipitação nulo para o talhão {FieldId}",
                                field.Id);
                            return;
                        }

                        var sensorData = new SensorDataRequestDto(
                            FieldId: field.Id,
                            SensorType: "Rainfall",
                            Value: rainValue.Value,
                            Timestamp: DateTime.UtcNow
                        );

                        await sensorService.SendSensorDataAsync(sensorData, stoppingToken);

                        _logger.LogDebug(
                            "Dados de precipitação enviados: FieldId={FieldId}, Value={Value}mm",
                            field.Id, rainValue.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Erro ao processar talhão {FieldId} para precipitação",
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
