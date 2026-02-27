using Application.DTOs;

namespace Application.Services.Interfaces;

public interface ISensorDataService
{
    Task<bool> SendSensorDataAsync(SensorDataRequestDto data, CancellationToken cancellationToken = default);
}
