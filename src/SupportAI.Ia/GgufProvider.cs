using System.Diagnostics;
using System.Text.Json;
using SupportAI.Core.Models;

namespace SupportAI.Ia;

public class GgufProvider : ILlmProvider
{
    private readonly string _modelsDir;

    public GgufProvider(string? modelsDir = null)
    {
        _modelsDir = modelsDir ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");
    }

    public string Name => "IA Local (GGUF)";
    public bool Disponible => File.Exists(LlamaCliPath) && Directory.GetFiles(_modelsDir, "*.gguf").Length > 0;

    private string LlamaCliPath => Path.Combine(_modelsDir, "llama-cli.exe");
    private string? FindModel() => Directory.GetFiles(_modelsDir, "*.gguf").FirstOrDefault();

    public async Task<LlmResponse> AnalyzeAsync(Diagnostico diag, CancellationToken ct)
    {
        var modelPath = FindModel();
        if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(LlamaCliPath))
            throw new InvalidOperationException("Modelo GGUF no encontrado. Descárgalo desde la UI.");

        var json = JsonSerializer.Serialize(diag, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var promptTemplate = """
<s>[INST] Eres un técnico experto en diagnóstico de Windows. Analiza estos datos y responde SOLO con JSON:
{
  "explicacion": "causa raíz en español",
  "recomendaciones": [
    { "accion": "acción", "comando": "comando powershell", "detalle": "por qué" }
  ]
}

Datos:
""";
        var prompt = promptTemplate + json + "\n[/INST]";
        var tempFile = Path.Combine(Path.GetTempPath(), $"llama_prompt_{Guid.NewGuid()}.txt");

        try
        {
            await File.WriteAllTextAsync(tempFile, prompt, ct);

            var psi = new ProcessStartInfo
            {
                FileName = LlamaCliPath,
                Arguments = $"-m \"{modelPath}\" -f \"{tempFile}\" -n 1000 --temp 0.3 --no-display-prompt",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            var jsonStart = output.IndexOf('{');
            var jsonEnd = output.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd <= jsonStart)
                throw new InvalidOperationException("No se pudo parsear la respuesta del modelo.");

            output = output[jsonStart..(jsonEnd + 1)];
            return OpenRouterProvider.ParseResponseStatic(output, Name);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignorar errores al borrar el archivo temporal
                }
            }
        }
    }
}
