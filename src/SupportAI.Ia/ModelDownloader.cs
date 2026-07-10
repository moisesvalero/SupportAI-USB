using System.IO.Compression;
using System.Net.Http;

namespace SupportAI.Ia;

public static class ModelDownloader
{
    public const string TinyModelUrl = "https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF/resolve/main/qwen2.5-0.5b-instruct-q4_k_m.gguf";
    public const string TinyModelName = "qwen2.5-0.5b-instruct-q4_k_m.gguf";
    public const long TinyModelSizeMB = 350;

    public static string ModelsDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");

    public static bool ModelExists =>
        Directory.Exists(ModelsDir) &&
        Directory.GetFiles(ModelsDir, "*.gguf").Length > 0 &&
        File.Exists(Path.Combine(ModelsDir, "llama-cli.exe"));

    public static string? CurrentModelPath =>
        Directory.Exists(ModelsDir)
            ? Directory.GetFiles(ModelsDir, "*.gguf").FirstOrDefault()
            : null;

    public static string GetStatus()
    {
        if (!Directory.Exists(ModelsDir))
            return "No hay carpeta models/. Usa 'Descargar' para obtener un modelo.";
        var cliExists = File.Exists(Path.Combine(ModelsDir, "llama-cli.exe"));
        var ggufFiles = Directory.GetFiles(ModelsDir, "*.gguf");
        if (!cliExists && ggufFiles.Length == 0)
            return "Descarga el modelo desde la app.";
        if (!cliExists)
            return "Falta llama-cli.exe. Reintenta la descarga.";
        if (ggufFiles.Length == 0)
            return "Falta modelo .gguf. Descarga uno desde la app.";
        return $"Listo: {Path.GetFileName(ggufFiles[0])} ({(new FileInfo(ggufFiles[0]).Length / 1073741824.0):F1} GB)";
    }

    public static async Task DownloadTinyModelAsync(IProgress<int> progress, CancellationToken ct)
    {
        Directory.CreateDirectory(ModelsDir);
        var modelPath = Path.Combine(ModelsDir, TinyModelName);
        var cliZipPath = Path.Combine(ModelsDir, "llama.zip");
        var cliExePath = Path.Combine(ModelsDir, "llama-cli.exe");

        // 1. Download llama-cli.exe zip (0-30%)
        if (!File.Exists(cliExePath))
        {
            progress.Report(0);
            var zipUrl = "https://github.com/ggerganov/llama.cpp/releases/download/b4741/llama-b4741-bin-win-cuda-cu12.5.7z";
            try
            {
                await DownloadFileAsync(zipUrl, cliZipPath, progress, ct, 0, 28);

                progress.Report(29);
                // Try 7z first, then fallback
                if (!await ExtractExeFrom7zAsync(cliZipPath, ModelsDir))
                {
                    // Fallback: open browser for manual download
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/ggerganov/llama.cpp/releases/latest",
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
                // If download fails, skip CLI - user can still use RulesProvider
            }
            finally
            {
                try { if (File.Exists(cliZipPath)) File.Delete(cliZipPath); } catch { }
            }
        }
        progress.Report(30);

        // 2. Download GGUF model (30-100%)
        if (!File.Exists(modelPath))
        {
            await DownloadFileAsync(TinyModelUrl, modelPath, progress, ct, 30, 100);
        }

        progress.Report(100);
    }

    private static async Task DownloadFileAsync(string url, string destPath, IProgress<int> progress, CancellationToken ct, int minPct, int maxPct)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength ?? 1;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
        var buffer = new byte[81920];
        long bytesReadSoFar = 0;
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            bytesReadSoFar += bytesRead;
            var pct = minPct + (int)((double)bytesReadSoFar / totalBytes * (maxPct - minPct));
            progress.Report(Math.Clamp(pct, minPct, maxPct));
        }
    }

    private static async Task<bool> ExtractExeFrom7zAsync(string zipPath, string outputDir)
    {
        // Try 7z
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "7z",
                Arguments = $"e \"{zipPath}\" -o\"{outputDir}\" llama-cli.exe -y",
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            using var proc = new System.Diagnostics.Process { StartInfo = psi };
            proc.Start(); await proc.WaitForExitAsync();
            if (proc.ExitCode == 0 && File.Exists(Path.Combine(outputDir, "llama-cli.exe")))
                return true;
        }
        catch { }
        return false;
    }
}
