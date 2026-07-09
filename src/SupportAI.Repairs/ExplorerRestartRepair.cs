using System.Diagnostics;

namespace SupportAI.Repairs;

public class ExplorerRestartRepair : CommandRepair
{
    public override string Id => "rep.explorer.restart";
    public override string Titulo => "Reiniciar Explorer";
    public override string Descripcion => "Reinicia el proceso Explorer.exe (interfaz de Windows). Útil tras cambios de registro o cuelgues de interfaz.";
    public override string Comando => "taskkill /f /im explorer.exe & start explorer.exe";

    public override async Task<RepairResult> ExecuteAsync(bool dryRun = false, CancellationToken ct = default)
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
        await killProcess.WaitForExitAsync(ct);

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
