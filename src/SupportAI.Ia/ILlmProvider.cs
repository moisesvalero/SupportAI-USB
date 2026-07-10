using SupportAI.Core.Models;

namespace SupportAI.Ia;

public interface ILlmProvider
{
    string Name { get; }
    bool Disponible { get; }
    Task<LlmResponse> AnalyzeAsync(Diagnostico diag, CancellationToken ct = default);
    Task<string> ChatAsync(List<(string Role, string Text)> messages, CancellationToken ct = default) =>
        throw new NotSupportedException("Chat no soportado por este proveedor.");
}

public record LlmResponse(
    string Explicacion,
    List<LlmRecomendacion> Recomendaciones,
    string ProveedorUsado
);

public record LlmRecomendacion(string Accion, string Comando, string Detalle);
