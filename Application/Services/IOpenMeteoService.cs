using Application.DTOs;

namespace Application.Services;

public interface IOpenMeteoService
{
    Task<OpenMeteoResponseDto?> GetWeatherDataAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
