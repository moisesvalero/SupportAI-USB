namespace SupportAI.Ia;

public static class ModelDownloader
{
    public static readonly string LlamaCppUrl = "https://github.com/ggerganov/llama.cpp/releases/latest";
    public static readonly string RecommendedModelUrl =
        "https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf";
    public static readonly string RecommendedModelName = "Llama-3.2-3B-Instruct-Q4_K_M.gguf";
    public static readonly long RecommendedModelSizeMB = 2000;

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
            return "Carpeta models/ no encontrada.";

        var cliExists = File.Exists(Path.Combine(ModelsDir, "llama-cli.exe"));
        var ggufFiles = Directory.GetFiles(ModelsDir, "*.gguf");

        if (!cliExists && ggufFiles.Length == 0)
            return "Descarga llama.cpp y un modelo .gguf en la carpeta models/.";
        if (!cliExists)
            return "Falta llama-cli.exe. Descárgalo desde GitHub.";
        if (ggufFiles.Length == 0)
            return "Falta modelo .gguf. Descarga uno compatible.";
        return $"Listo: {Path.GetFileName(ggufFiles[0])} ({(new FileInfo(ggufFiles[0]).Length / 1073741824.0):F1} GB)";
    }
}
