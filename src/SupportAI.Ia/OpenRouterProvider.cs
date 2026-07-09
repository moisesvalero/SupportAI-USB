using System.Net.Http.Json;
using System.Text.Json;
using SupportAI.Core.Models;

namespace SupportAI.Ia;

public class OpenRouterProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public OpenRouterProvider(string apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _http.DefaultRequestHeaders.Add("HTTP-Referer", "https://supportai-usb.local");
        _http.DefaultRequestHeaders.Add("X-OpenRouter-Title", "SupportAI USB");
    }

    public string Name => "OpenRouter";
    public bool Disponible => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<LlmResponse> AnalyzeAsync(Diagnostico diag, CancellationToken ct)
    {
        var prompt = BuildPrompt(diag);
        var body = new
        {
            model = "openrouter/free",
            messages = new[]
            {
                new { role = "system", content = "Eres un técnico experto en diagnóstico de Windows. Analiza los datos y responde SOLO con JSON." },
                new { role = "user", content = prompt }
            },
            max_tokens = 2000,
            temperature = 0.3
        };

        var response = await _http.PostAsJsonAsync(
            "https://openrouter.ai/api/v1/chat/completions", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<OpenRouterResponse>(ct);
        var content = json?.choices?[0]?.message?.content;
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("Respuesta vacía de OpenRouter");

        return ParseResponseStatic(content, Name);
    }

    private static string BuildPrompt(Diagnostico diag)
    {
        var json = JsonSerializer.Serialize(diag, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var template = """
Analiza este diagnóstico de Windows y responde ÚNICAMENTE con JSON sin marcas:
{
  "explicacion": "texto breve de causa raíz en español",
  "recomendaciones": [
    { "accion": "nombre acción", "comando": "comando powershell", "detalle": "por qué" }
  ]
}

Diagnóstico:
""";
        return template + json;
    }

    internal static LlmResponse ParseResponseStatic(string json, string provider)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var explicacion = root.TryGetProperty("explicacion", out var exp)
            ? exp.GetString() ?? ""
            : "";

        var recomendaciones = new List<LlmRecomendacion>();
        if (root.TryGetProperty("recomendaciones", out var recs))
        {
            foreach (var r in recs.EnumerateArray())
            {
                recomendaciones.Add(new LlmRecomendacion(
                    r.TryGetProperty("accion", out var a) ? a.GetString() ?? "" : "",
                    r.TryGetProperty("comando", out var c) ? c.GetString() ?? "" : "",
                    r.TryGetProperty("detalle", out var d) ? d.GetString() ?? "" : ""
                ));
            }
        }

        return new LlmResponse(explicacion, recomendaciones, provider);
    }

    private class OpenRouterResponse
    {
        public Choice[]? choices { get; set; }
    }
    private class Choice
    {
        public Message? message { get; set; }
    }
    private class Message
    {
        public string? content { get; set; }
    }
}
