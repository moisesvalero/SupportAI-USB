namespace SupportAI.Repairs;

public class TempCleanRepair : CommandRepair
{
    public override string Id => "rep.temp.clean";
    public override string Titulo => "Limpiar archivos temporales";
    public override string Descripcion => "Elimina archivos temporales de %TEMP% y C:\\Windows\\Temp.";

        public override bool RequiresElevation => true;

    public override string Comando => @"
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
".Trim();
}
