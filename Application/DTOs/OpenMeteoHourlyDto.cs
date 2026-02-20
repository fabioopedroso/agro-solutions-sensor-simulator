using System.Text.Json.Serialization;

namespace Application.DTOs;

public class OpenMeteoHourlyDto
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("temperature_2m")]
    public List<double?> Temperature2m { get; set; } = new();

    [JsonPropertyName("rain")]
    public List<double?> Rain { get; set; } = new();

    [JsonPropertyName("soil_moisture_0_to_7cm")]
    public List<double?> SoilMoisture0To7cm { get; set; } = new();
}
