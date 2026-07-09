namespace SupportAI.Repairs;

public class SfcRepair : CommandRepair
{
    public override string Id => "rep.sfc";
    public override string Titulo => "Reparar archivos del sistema";
    public override string Descripcion => "Ejecuta sfc /scannow para verificar y reparar archivos protegidos del sistema.";
    public override string Comando => "sfc /scannow";
    protected override string FileName => "sfc.exe";
    protected override string Arguments => "/scannow";
}
