using System.Text.Json.Serialization;

namespace SupportAI.Core.Models;

public record NetworkInfo
{
    [JsonPropertyName("dns")] public string? DNS { get; init; }
    [JsonPropertyName("gateway")] public string? Gateway { get; init; }
    [JsonPropertyName("adaptadores")] public List<AdaptadorInfo> Adaptadores { get; init; } = [];
    [JsonPropertyName("internet")] public bool Internet { get; init; }
    [JsonPropertyName("latenciaMs")] public double LatenciaMs { get; init; }
}

public record AdaptadorInfo
{
    [JsonPropertyName("nombre")] public string Nombre { get; init; } = "";
    [JsonPropertyName("ip")] public string? IP { get; init; }
    [JsonPropertyName("dhcpActivo")] public bool DhcpActivo { get; init; }
    [JsonPropertyName("tipo")] public string Tipo { get; init; } = "";
}
