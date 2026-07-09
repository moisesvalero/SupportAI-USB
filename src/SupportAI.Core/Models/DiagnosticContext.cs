namespace SupportAI.Core.Models;

public class DiagnosticContext
{
    public Diagnostico Diagnostico { get; set; } = new();
    public List<string> Log { get; } = [];

    public void LogInfo(string message)
    {
        Log.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}
