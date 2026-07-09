using System.Diagnostics;

namespace SupportAI.Repairs;

public class TempCleanRepair : IRepairAction
{
    public string Id => "rep.temp.clean";
    public string Titulo => "Limpiar archivos temporales";
    public string Descripcion => "Elimina archivos temporales de %TEMP% y C:\\Windows\\Temp.";
    public string Comando => @"
del /q /s %TEMP%\* 2>nul
del /q /s C:\Windows\Temp\* 2>nul
".Trim();

    public async Task<RepairResult> ExecuteAsync(bool dryRun = false)
    {
        if (dryRun)
            return new RepairResult(true, $"[Dry-run] {Comando}");

        var script = @"
$folders = @(
    [System.IO.Path]::GetTempPath(),
    'C:\Windows\Temp'
)
$total = 0
foreach ($f in $folders) {
    if (Test-Path $f) {
        Get-ChildItem $f -Recurse -Force -ErrorAction SilentlyContinue | Where-Object { !$_.PSIsContainer -and $_.LastWriteTime -lt (Get-Date).AddHours(-1) } | ForEach-Object {
            try { $total += $_.Length; Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue } catch {}
        }
    }
}
Write-Output ""Limpiados $([math]::Round($total / 1MB, 2)) MB""
";
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
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
