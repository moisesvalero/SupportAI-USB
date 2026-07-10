namespace SupportAI.Repairs;

public class DismRepair : CommandRepair
{
    public override string Id => "rep.dism.health";
    public override string Titulo => "Restaurar imagen de Windows";
    public override string Descripcion => "Ejecuta DISM /Online /Cleanup-Image /RestoreHealth para reparar la imagen del sistema.";
    public override bool RequiresElevation => true;
    public override string Comando => "DISM /Online /Cleanup-Image /RestoreHealth";
    protected override string FileName => "DISM.exe";
    protected override string Arguments => "/Online /Cleanup-Image /RestoreHealth";
}
