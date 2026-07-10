namespace SupportAI.Repairs;

public class WinsockResetRepair : CommandRepair
{
    public override string Id => "rep.winsock.reset";
    public override string Titulo => "Resetear Winsock";
    public override string Descripcion => "Reinicia la pila Winsock y restaura configuración de red a valores por defecto.";
    public override bool RequiresElevation => true;
    public override string Comando => "netsh winsock reset";
}
