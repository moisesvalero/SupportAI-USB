using System.Text.Json.Serialization;

namespace SupportAI.Core.Models;

public record WindowsInfo
{
    [JsonPropertyName("updatePendiente")] public bool UpdatePendiente { get; init; }
    [JsonPropertyName("archivosCorruptos")] public bool ArchivosCorruptos { get; init; }
    [JsonPropertyName("serviciosFallando")] public List<ServicioInfo> ServiciosFallando { get; init; } = [];
    [JsonPropertyName("eventosCriticos")] public List<EventoInfo> EventosCriticos { get; init; } = [];
}

public record ServicioInfo
{
    [JsonPropertyName("nombre")] public string Nombre { get; init; } = "";
    [JsonPropertyName("nombreCorto")] public string NombreCorto { get; init; } = "";
    [JsonPropertyName("estado")] public string Estado { get; init; } = "";
    [JsonPropertyName("tipoInicio")] public string TipoInicio { get; init; } = "";
    [JsonPropertyName("pathName")] public string? PathName { get; init; }
}

public record EventoInfo
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("nivel")] public int Nivel { get; init; }
    [JsonPropertyName("fuente")] public string Fuente { get; init; } = "";
    [JsonPropertyName("mensaje")] public string Mensaje { get; init; } = "";
    [JsonPropertyName("timestamp")] public DateTime? Timestamp { get; init; }
}
