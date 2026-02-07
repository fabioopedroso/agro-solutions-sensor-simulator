namespace Application.DTOs;

public class SensorSimulatorSettings
{
    public SensorIngestionSettings SensorIngestion { get; set; } = new();
    public AuthenticationSettings Authentication { get; set; } = new();
    public SimulationSettings Simulation { get; set; } = new();
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

public class SimulationSettings
{
    public int[] FieldIds { get; set; } = Array.Empty<int>();
    public int IntervalSeconds { get; set; } = 45;
    public SensorConfig SoilHumidity { get; set; } = new();
    public SensorConfig Temperature { get; set; } = new();
    public SensorConfig Rainfall { get; set; } = new();
}

public class SensorConfig
{
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double Delta { get; set; }
    public double InitialValue { get; set; }
}
