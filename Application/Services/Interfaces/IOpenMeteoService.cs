using Application.DTOs;

namespace Application.Services.Interfaces;

public interface IOpenMeteoService
{
    Task<OpenMeteoResponseDto?> GetWeatherDataAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
