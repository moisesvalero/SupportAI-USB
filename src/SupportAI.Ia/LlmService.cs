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

        var gguf = new GgufProvider();
        if (gguf.Disponible)
            _providers.Insert(0, gguf);
    }

    public LlmService(List<ILlmProvider> providers, RulesProvider rules)
    {
        _providers = providers;
        _rules = rules;
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
                if (!string.IsNullOrWhiteSpace(result.Explicacion))
                    return result with { ProveedorUsado = provider.Name };
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                System.Diagnostics.Trace.WriteLine($"[LlmService] Error al analizar con {provider.Name}: {ex.Message}");
                if (ct.IsCancellationRequested && (ex is OperationCanceledException || ex is TaskCanceledException))
                    throw;
            }
        }

        return await _rules.AnalyzeAsync(anonymized, ct);
    }

    public async Task<string> ChatAsync(List<(string Role, string Text)> messages, CancellationToken ct = default)
    {
        var errores = new List<string>();
        foreach (var provider in _providers)
        {
            if (!provider.Disponible) continue;
            if (provider is RulesProvider) continue;
            try
            {
                return await provider.ChatAsync(messages, ct);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                errores.Add($"{provider.Name}: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"[LlmService] Chat error con {provider.Name}: {ex.Message}");
            }
        }

        if (errores.Count > 0)
            return $"⚠️ Error en todos los proveedores IA:\n\n{string.Join("\n", errores)}\n\nRevisa tus API keys en ⚙️ o descarga el modelo GGUF desde el panel lateral.";

        return "⚠️ No hay proveedor IA disponible. Ve a ⚙️ para añadir una API key de OpenRouter/Gemini o descarga el modelo local (350 MB) desde el panel lateral.";
    }
}
