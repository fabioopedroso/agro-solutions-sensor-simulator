using Application.DTOs;
using Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Application.Services;

public class OpenMeteoService : IOpenMeteoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenMeteoService> _logger;
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenMeteoService(HttpClient httpClient, ILogger<OpenMeteoService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<OpenMeteoResponseDto?> GetWeatherDataAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validação de coordenadas
            if (latitude < -90 || latitude > 90)
            {
                _logger.LogWarning("Latitude inválida: {Latitude}", latitude);
                return null;
            }

            if (longitude < -180 || longitude > 180)
            {
                _logger.LogWarning("Longitude inválida: {Longitude}", longitude);
                return null;
            }

            var url = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&hourly=temperature_2m,rain,soil_moisture_0_to_7cm&timezone=auto";

            _logger.LogInformation("Buscando dados meteorológicos para coordenadas: Lat={Latitude}, Lon={Longitude}", latitude, longitude);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Erro ao buscar dados da API Open-Meteo. StatusCode={StatusCode}, Error={Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<OpenMeteoResponseDto>(_jsonOptions, cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("Resposta da API Open-Meteo está vazia para coordenadas: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
                return null;
            }

            _logger.LogInformation(
                "Dados meteorológicos obtidos com sucesso. Timezone={Timezone}, Dados horários={Count}",
                result.Timezone, result.Hourly.Time.Count);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de rede ao buscar dados da API Open-Meteo para coordenadas: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout ao buscar dados da API Open-Meteo para coordenadas: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar resposta da API Open-Meteo para coordenadas: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar dados da API Open-Meteo para coordenadas: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
            return null;
        }
    }
}
