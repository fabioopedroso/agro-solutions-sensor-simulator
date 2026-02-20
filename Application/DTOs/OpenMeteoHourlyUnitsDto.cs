using System.Text.Json.Serialization;

namespace Application.DTOs;

public class OpenMeteoHourlyUnitsDto
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public string Temperature2m { get; set; } = string.Empty;

    [JsonPropertyName("rain")]
    public string Rain { get; set; } = string.Empty;

    [JsonPropertyName("soil_moisture_0_to_7cm")]
    public string SoilMoisture0To7cm { get; set; } = string.Empty;
}
