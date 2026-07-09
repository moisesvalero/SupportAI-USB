using System.Diagnostics;

namespace SupportAI.Repairs;

public abstract class CommandRepair : IRepairAction
{
    public abstract string Id { get; }
    public abstract string Titulo { get; }
    public abstract string Descripcion { get; }
    public abstract string Comando { get; }
    protected virtual string FileName => "powershell.exe";
    protected virtual string Arguments => $"-NoProfile -ExecutionPolicy Bypass -Command \"{Comando.Replace("\"", "\\\"")}\"";

    public virtual async Task<RepairResult> ExecuteAsync(bool dryRun = false, CancellationToken ct = default)
    {
        if (dryRun)
            return new RepairResult(true, $"[Dry-run] {Comando}");

        var psi = new ProcessStartInfo
        {
            FileName = FileName,
            Arguments = Arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = new Process { StartInfo = psi };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return RepairResult.FromProcess(process.ExitCode, output, error);
    }
}
