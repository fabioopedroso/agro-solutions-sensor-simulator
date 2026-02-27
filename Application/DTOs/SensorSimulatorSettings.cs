namespace Application.DTOs;

public class SensorSimulatorSettings
{
    public SensorIngestionSettings SensorIngestion { get; set; } = new();
    public AuthenticationSettings Authentication { get; set; } = new();
    public WorkersSettings Workers { get; set; } = new();
}

public class SensorIngestionSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "/api/sensor-data";
}

public class AuthenticationSettings
{
    public string Token { get; set; } = string.Empty;
}

public class WorkersSettings
{
    public int IntervalSeconds { get; set; } = 45;
}
