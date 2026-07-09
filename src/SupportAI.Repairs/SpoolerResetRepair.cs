namespace SupportAI.Repairs;

public class SpoolerResetRepair : CommandRepair
{
    public override string Id => "rep.spooler.reset";
    public override string Titulo => "Reiniciar servicio de impresión";
    public override string Descripcion => "Detiene y reinicia el spooler de impresión, limpiando trabajos atascados.";
    public override string Comando => @"
Stop-Service spooler -Force
Remove-Item 'C:\Windows\System32\spool\PRINTERS\*' -Force -ErrorAction SilentlyContinue
Start-Service spooler
Write-Output 'Spooler reiniciado y cola limpiada.'
";
}
