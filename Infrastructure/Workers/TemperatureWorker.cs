using Application.DTOs;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Workers;

public class TemperatureWorker : BackgroundService
{
    private readonly ILogger<TemperatureWorker> _logger;
    private readonly SensorSimulatorSettings _settings;
    private readonly IServiceProvider _serviceProvider;

    public TemperatureWorker(
        ILogger<TemperatureWorker> logger,
        IOptions<SensorSimulatorSettings> settings,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TemperatureWorker iniciado");

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

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

                _logger.LogInformation("Processando {Count} talhões para temperatura", fieldsList.Count);

                // Processa cada talhão
                var tasks = fieldsList.Select(async field =>
                {
                    try
                    {
                        var weatherData = await openMeteoService.GetWeatherDataAsync(
                            field.Latitude,
                            field.Longitude,
                            stoppingToken);

                        if (weatherData == null || !weatherData.Hourly.Temperature2m.Any())
                        {
                            _logger.LogWarning(
                                "Não foi possível obter dados de temperatura para o talhão {FieldId}",
                                field.Id);
                            return;
                        }

                        // Extrai o valor mais recente (primeiro índice)
                        var temperatureValue = weatherData.Hourly.Temperature2m[0];

                        if (temperatureValue == null)
                        {
                            _logger.LogWarning(
                                "Valor de temperatura nulo para o talhão {FieldId}",
                                field.Id);
                            return;
                        }

                        var sensorData = new SensorDataRequestDto(
                            FieldId: field.Id,
                            SensorType: "Temperature",
                            Value: temperatureValue.Value,
                            Timestamp: DateTime.UtcNow
                        );

                        await sensorService.SendSensorDataAsync(sensorData, stoppingToken);

                        _logger.LogDebug(
                            "Dados de temperatura enviados: FieldId={FieldId}, Value={Value}°C",
                            field.Id, temperatureValue.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Erro ao processar talhão {FieldId} para temperatura",
                            field.Id);
                    }
                });

                await Task.WhenAll(tasks);

                // Atualiza cache de talhões para detectar novos
                await fieldService.RefreshFieldsCacheAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(_settings.Workers.IntervalSeconds), stoppingToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("Não foi possível conectar aos serviços externos: {Message}. Tentando novamente em 30 segundos...", ex.Message);
                // Espera um pouco mais em caso de erro de rede para não inundar o log
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TemperatureWorker cancelado");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no TemperatureWorker");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("TemperatureWorker finalizado");
    }
}
