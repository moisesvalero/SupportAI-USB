using System.Text.Json.Serialization;

namespace SupportAI.Core.Models;

public record Diagnostico
{
    [JsonPropertyName("version")] public string Version { get; init; } = "1.0";
    [JsonPropertyName("generadoEn")] public DateTime GeneradoEn { get; init; } = DateTime.UtcNow;
    [JsonPropertyName("hardware")] public HardwareInfo? Hardware { get; init; }
    [JsonPropertyName("salud")] public HealthInfo? Salud { get; init; }
    [JsonPropertyName("windows")] public WindowsInfo? Windows { get; init; }
    [JsonPropertyName("red")] public NetworkInfo? Red { get; init; }
    [JsonPropertyName("seguridad")] public SecurityInfo? Seguridad { get; init; }
    [JsonPropertyName("drivers")] public DriverInfo? Drivers { get; init; }
    [JsonPropertyName("problemas")] public List<Problema> Problemas { get; init; } = [];
    [JsonPropertyName("puntuacion")] public int Puntuacion { get; init; }
}
