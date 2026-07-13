using System.ComponentModel;
using System.Diagnostics;

namespace SupportAI.Repairs;

public abstract class CommandRepair : IRepairAction
{
    public abstract string Id { get; }
    public abstract string Titulo { get; }
    public abstract string Descripcion { get; }
    public abstract string Comando { get; }
    public virtual bool RequiresElevation => false;
    protected virtual string FileName => "powershell.exe";
    protected virtual string Arguments
    {
        get
        {
            if (FileName.EndsWith("powershell.exe", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = System.Text.Encoding.Unicode.GetBytes(Comando);
                var encoded = Convert.ToBase64String(bytes);
                return $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}";
            }
            return Comando;
        }
    }

    public virtual async Task<RepairResult> ExecuteAsync(bool dryRun = false, CancellationToken ct = default)
    {
        if (dryRun)
            return new RepairResult(true, $"[Dry-run] {Comando}");

        try
        {
            return RequiresElevation
                ? await RunElevatedAsync(ct)
                : await RunRedirectedAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new RepairResult(false, "", "Operación cancelada por el usuario (UAC denegado).");
        }
        catch (Win32Exception ex)
        {
            return new RepairResult(false, "", $"No se pudo iniciar el proceso: {ex.Message}");
        }
    }

    private async Task<RepairResult> RunRedirectedAsync(CancellationToken ct)
    {
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
        var readOutputTask = process.StandardOutput.ReadToEndAsync(ct);
        var readErrorTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        
        var output = await readOutputTask;
        var error = await readErrorTask;
        return RepairResult.FromProcess(process.ExitCode, output, error);
    }

    private async Task<RepairResult> RunElevatedAsync(CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = FileName,
            Arguments = Arguments,
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = false
        };
        using var process = new Process { StartInfo = psi };
        process.Start();
        await process.WaitForExitAsync(ct);
        return process.ExitCode == 0
            ? new RepairResult(true, $"{Titulo} ejecutado con permisos elevados.")
            : new RepairResult(false, "", $"{Titulo} finalizado con código {process.ExitCode}.");
    }
}
