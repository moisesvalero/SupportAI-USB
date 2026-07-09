using System.Text.Json.Serialization;

namespace SupportAI.Core.Models;

public record SecurityInfo
{
    [JsonPropertyName("defenderActivo")] public bool DefenderActivo { get; init; }
    [JsonPropertyName("firewallActivo")] public bool FirewallActivo { get; init; }
    [JsonPropertyName("bitlockerActivo")] public bool BitlockerActivo { get; init; }
    [JsonPropertyName("ultimoAnalisis")] public string? UltimoAnalisis { get; init; }
}
