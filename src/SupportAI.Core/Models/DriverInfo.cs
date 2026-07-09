using System.Text.Json.Serialization;

namespace SupportAI.Core.Models;

public record DriverInfo
{
    [JsonPropertyName("dispositivosError")] public List<DispositivoErrorInfo> DispositivosError { get; init; } = [];
}

public record DispositivoErrorInfo
{
    [JsonPropertyName("nombre")] public string Nombre { get; init; } = "";
    [JsonPropertyName("codigoError")] public int CodigoError { get; init; }
    [JsonPropertyName("descripcion")] public string Descripcion { get; init; } = "";
}
