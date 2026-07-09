using System.Text.Json.Serialization;

namespace SupportAI.Core.Models;

public record Problema
{
    [JsonPropertyName("id")] public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    [JsonPropertyName("gravedad")] public Gravedad Gravedad { get; init; }
    [JsonPropertyName("modulo")] public string Modulo { get; init; } = "";
    [JsonPropertyName("titulo")] public string Titulo { get; init; } = "";
    [JsonPropertyName("detalle")] public string Detalle { get; init; } = "";
    [JsonPropertyName("reparacionesSugeridas")] public List<string> ReparacionesSugeridas { get; init; } = [];
    [JsonPropertyName("reparacionesAplicadas")] public List<string> ReparacionesAplicadas { get; init; } = [];
}

public enum Gravedad
{
    Bajo,
    Medio,
    Alto,
    Critico
}
