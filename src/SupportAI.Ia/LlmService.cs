using SupportAI.Core.Models;

namespace SupportAI.Ia;

public class LlmService
{
    private readonly List<ILlmProvider> _providers;
    private readonly RulesProvider _rules;

    public LlmService(string? openRouterKey = null, string? geminiKey = null)
    {
        _rules = new RulesProvider();
        _providers = [];

        if (!string.IsNullOrWhiteSpace(openRouterKey))
            _providers.Add(new OpenRouterProvider(openRouterKey));
        if (!string.IsNullOrWhiteSpace(geminiKey))
            _providers.Add(new GeminiProvider(geminiKey));

        _providers.Add(_rules);
    }

    public string ModoActual =>
        _providers.FirstOrDefault(p => p is not RulesProvider)?.Name ?? "Reglas locales";

    public bool ModoOnlineDisponible =>
        _providers.Any(p => p is not RulesProvider);

    public async Task<LlmResponse> AnalyzeAsync(Diagnostico diag, CancellationToken ct = default)
    {
        var anonymized = PrivacyFilter.Anonymize(diag);

        foreach (var provider in _providers)
        {
            if (!provider.Disponible) continue;
            try
            {
                var result = await provider.AnalyzeAsync(anonymized, ct);
                if (result.Recomendaciones.Count > 0)
                    return result with { ProveedorUsado = provider.Name };
            }
            catch
            {
                continue;
            }
        }

        return await _rules.AnalyzeAsync(diag, ct);
    }
}
