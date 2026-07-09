using System.Net.Http.Json;
using System.Text.Json;
using SupportAI.Core.Models;

namespace SupportAI.Ia;

public class GeminiProvider : ILlmProvider
{
    private readonly string _apiKey;
    private readonly HttpClient _http;

    public GeminiProvider(string apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public string Name => "Gemini (Google)";
    public bool Disponible => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<LlmResponse> AnalyzeAsync(Diagnostico diag, CancellationToken ct)
    {
        var prompt = BuildPrompt(diag);
        var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash:generateContent?key={_apiKey}";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 2000,
                temperature = 0.3
            }
        };

        var response = await _http.PostAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<GeminiResponse>(ct);
        var text = json?.candidates?[0]?.content?.parts?[0]?.text;
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Respuesta vacía de Gemini");

        // Extraer JSON del texto (Gemini a veces pone markdown)
        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
            text = text[jsonStart..(jsonEnd + 1)];

        return OpenRouterProvider.ParseResponseStatic(text, Name);
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

    private class GeminiResponse
    {
        public Candidate[]? candidates { get; set; }
    }
    private class Candidate
    {
        public GeminiContent? content { get; set; }
    }
    private class GeminiContent
    {
        public Part[]? parts { get; set; }
    }
    private class Part
    {
        public string? text { get; set; }
    }
}
