namespace SupportAI.Repairs;

public class RestorePointRepair : CommandRepair
{
    public override string Id => "rep.restore.point";
    public override string Titulo => "Crear punto de restauración";
    public override string Descripcion => "Crea un punto de restauración del sistema antes de aplicar reparaciones.";
    public override string Comando => @"
Checkpoint-Computer -Description 'SupportAI antes de reparación' -RestorePointType MODIFY_SETTINGS
Write-Output 'Punto de restauración creado correctamente.'
";
}
