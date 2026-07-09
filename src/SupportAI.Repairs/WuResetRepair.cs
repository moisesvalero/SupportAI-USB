namespace SupportAI.Repairs;

public class WuResetRepair : CommandRepair
{
    public override string Id => "rep.wu.reset";
    public override string Titulo => "Reiniciar Windows Update";
    public override string Descripcion => "Detiene servicios de Windows Update, borra caché y reinicia los servicios.";
    public override string Comando => @"
$services = @('wuauserv', 'cryptsvc', 'bits', 'msiserver')
$services | ForEach-Object { Stop-Service $_ -Force -ErrorAction SilentlyContinue }
Remove-Item 'C:\Windows\SoftwareDistribution\*' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item 'C:\Windows\System32\catroot2\*' -Recurse -Force -ErrorAction SilentlyContinue
$services | ForEach-Object { Start-Service $_ -ErrorAction SilentlyContinue }
Write-Output 'Windows Update cache limpiado y servicios reiniciados.'
";
}
