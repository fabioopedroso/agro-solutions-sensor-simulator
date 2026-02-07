using Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Application.Services;

public class SensorDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SensorDataService> _logger;
    private readonly SensorSimulatorSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;

    public SensorDataService(
        HttpClient httpClient,
        ILogger<SensorDataService> logger,
        IOptions<SensorSimulatorSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _httpClient.BaseAddress = new Uri(_settings.SensorIngestion.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(_settings.Authentication.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.Authentication.Token);
        }
    }

    public async Task<bool> SendSensorDataAsync(SensorDataRequestDto data, CancellationToken cancellationToken = default)
    {
        var maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation(
                    "Enviando dados do sensor: Tipo={SensorType}, Valor={Value}, FieldId={FieldId}, Tentativa={Retry}",
                    data.SensorType, data.Value, data.FieldId, retryCount + 1);

                var response = await _httpClient.PostAsJsonAsync(
                    _settings.SensorIngestion.Endpoint,
                    data,
                    _jsonOptions,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Dados do sensor enviados com sucesso: Tipo={SensorType}, StatusCode={StatusCode}",
                        data.SensorType, response.StatusCode);
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Falha ao enviar dados do sensor: Tipo={SensorType}, StatusCode={StatusCode}, Error={Error}",
                    data.SensorType, response.StatusCode, errorContent);

                retryCount++;

                if (retryCount < maxRetries)
                {
                    // Aguarda antes de tentar novamente (backoff exponencial)
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    _logger.LogInformation("Aguardando {Delay}s antes da próxima tentativa...", delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "Erro de rede ao enviar dados do sensor: Tipo={SensorType}, Tentativa={Retry}",
                    data.SensorType, retryCount + 1);

                retryCount++;

                if (retryCount < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro inesperado ao enviar dados do sensor: Tipo={SensorType}",
                    data.SensorType);
                return false;
            }
        }

        _logger.LogError(
            "Falha ao enviar dados do sensor após {MaxRetries} tentativas: Tipo={SensorType}",
            maxRetries, data.SensorType);
        return false;
    }
}
