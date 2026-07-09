namespace SupportAI.Repairs;

public class DnsFlushRepair : CommandRepair
{
    public override string Id => "rep.dns.flush";
    public override string Titulo => "Limpiar caché DNS";
    public override string Descripcion => "Ejecuta ipconfig /flushdns para limpiar la caché de resolución DNS.";
    public override string Comando => "ipconfig /flushdns";
    protected override string FileName => "ipconfig.exe";
    protected override string Arguments => "/flushdns";
}
