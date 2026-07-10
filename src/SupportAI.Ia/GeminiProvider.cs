using System.Net.Http.Json;
using System.Text.Json;
using SupportAI.Core.Models;

namespace SupportAI.Ia;

public class GeminiProvider : ILlmProvider
{
    private static readonly HttpClient _sharedHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    private readonly string _apiKey;

    public GeminiProvider(string apiKey)
    {
        _apiKey = apiKey;
    }

    public string Name => "Gemini (Google)";
    public bool Disponible => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<string> ChatAsync(List<(string Role, string Text)> messages, CancellationToken ct = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={_apiKey}";

        var contents = messages.Where(m => m.Role != "system").Select(m => new
        {
            role = m.Role == "assistant" ? "model" : "user",
            parts = new[] { new { text = m.Text } }
        });

        var systemText = messages.FirstOrDefault(m => m.Role == "system").Text ?? "";

        var body = new
        {
            systemInstruction = new { parts = new[] { new { text = systemText } } },
            contents,
            generationConfig = new { maxOutputTokens = 1000, temperature = 0.3 }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        var response = await _sharedHttp.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<GeminiResponse>(ct);
        var text = json?.candidates?[0]?.content?.parts?[0]?.text;
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Respuesta vacía de Gemini");
        return text;
    }

    public async Task<LlmResponse> AnalyzeAsync(Diagnostico diag, CancellationToken ct)
    {
        var prompt = BuildPrompt(diag);
        var url = "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent";

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

        HttpResponseMessage? response = null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add("x-goog-api-key", _apiKey);

            response = await _sharedHttp.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                break;

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                continue;
            }

            break;
        }

        if (response == null)
            throw new InvalidOperationException("No se pudo obtener respuesta de Gemini.");

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
