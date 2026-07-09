using System.Diagnostics;

namespace SupportAI.Repairs;

public class ExplorerRestartRepair : IRepairAction
{
    public string Id => "rep.explorer.restart";
    public string Titulo => "Reiniciar Explorer";
    public string Descripcion => "Reinicia el proceso Explorer.exe (interfaz de Windows). Útil tras cambios de registro o cuelgues de interfaz.";
    public string Comando => "taskkill /f /im explorer.exe & start explorer.exe";

    public async Task<RepairResult> ExecuteAsync(bool dryRun = false)
    {
        if (dryRun)
            return new RepairResult(true, $"[Dry-run] {Comando}");

        // Kill explorer
        var psiKill = new ProcessStartInfo
        {
            FileName = "taskkill.exe",
            Arguments = "/f /im explorer.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var killProcess = new Process { StartInfo = psiKill };
        killProcess.Start();
        await killProcess.WaitForExitAsync();

        // Start explorer
        var psiStart = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = true,
            CreateNoWindow = true
        };
        Process.Start(psiStart);

        return new RepairResult(true, "Explorer reiniciado correctamente.");
    }
}
