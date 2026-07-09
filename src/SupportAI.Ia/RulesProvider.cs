using SupportAI.Core.Models;

namespace SupportAI.Ia;

public class RulesProvider : ILlmProvider
{
    public string Name => "Reglas locales";
    public bool Disponible => true;

    public Task<LlmResponse> AnalyzeAsync(Diagnostico diag, CancellationToken ct = default)
    {
        var recomendaciones = new List<LlmRecomendacion>();

        if (diag.Problemas.Count == 0)
        {
            return Task.FromResult(new LlmResponse(
                "No se detectaron problemas significativos en el sistema.",
                recomendaciones, Name));
        }

        foreach (var p in diag.Problemas)
        {
            foreach (var r in p.ReparacionesSugeridas)
            {
                recomendaciones.Add(RepairToRec(r));
            }
        }

        var explicacion = $"""
Se detectaron {diag.Problemas.Count} problema(s) en el equipo.
Los más críticos están en los módulos: {
    string.Join(", ", diag.Problemas
        .Where(p => p.Gravedad >= Gravedad.Alto)
        .Select(p => p.Modulo)
        .Distinct()
        .DefaultIfEmpty("ninguno"))
}.

Se recomienda revisar cada problema y aplicar las reparaciones sugeridas.
""";

        return Task.FromResult(new LlmResponse(explicacion.Trim(), recomendaciones, Name));
    }

    private static LlmRecomendacion RepairToRec(string id)
    {
        var repair = Repairs.RepairCatalog.Get(id);
        if (repair is not null)
            return new LlmRecomendacion(repair.Titulo, repair.Comando, repair.Descripcion);
        return new LlmRecomendacion(id, "", "");
    }
}
