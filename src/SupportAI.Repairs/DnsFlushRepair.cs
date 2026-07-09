using System.Diagnostics;

namespace SupportAI.Repairs;

public class DnsFlushRepair : IRepairAction
{
    public string Id => "rep.dns.flush";
    public string Titulo => "Limpiar caché DNS";
    public string Descripcion => "Ejecuta ipconfig /flushdns para limpiar la caché de resolución DNS.";
    public string Comando => "ipconfig /flushdns";

    public async Task<RepairResult> ExecuteAsync(bool dryRun = false)
    {
        if (dryRun)
            return new RepairResult(true, $"[Dry-run] {Comando}");

        var psi = new ProcessStartInfo
        {
            FileName = "ipconfig.exe",
            Arguments = "/flushdns",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = new Process { StartInfo = psi };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return RepairResult.FromProcess(process.ExitCode, output, error);
    }
}
