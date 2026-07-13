using System.Text;
using SupportAI.Core.Models;

namespace SupportAI.Ia;

public class RulesProvider : ILlmProvider
{
    public string Name => "Reglas locales";
    public bool Disponible => true;

    public Task<LlmResponse> AnalyzeAsync(Diagnostico diag, CancellationToken ct = default)
    {
        if (diag.Problemas.Count == 0)
        {
            return Task.FromResult(new LlmResponse(
                "No se detectaron problemas significativos en el sistema. El estado general es bueno. Sigue con el mantenimiento periódico: mantén Windows actualizado, no instales software de fuentes dudosas y revisa el estado del disco cada mes.",
                [], Name));
        }

        var sb = new StringBuilder();
        var recomendaciones = new List<LlmRecomendacion>();
        var criticos = diag.Problemas.Where(p => p.Gravedad >= Gravedad.Alto).ToList();
        var medios = diag.Problemas.Where(p => p.Gravedad == Gravedad.Medio).ToList();

        if (criticos.Count > 0)
        {
            sb.AppendLine("🔴 **PROBLEMAS CRÍTICOS**");
            sb.AppendLine($"Se han detectado {criticos.Count} problema(s) graves que requieren atención inmediata:\n");
            foreach (var p in criticos)
                AnalizarProblema(p, diag, sb, recomendaciones);
            sb.AppendLine();
        }

        if (medios.Count > 0)
        {
            sb.AppendLine("🟡 **PROBLEMAS MODERADOS**");
            sb.AppendLine($"Hay {medios.Count} problema(s) que deberías revisar:\n");
            foreach (var p in medios)
                AnalizarProblema(p, diag, sb, recomendaciones);
            sb.AppendLine();
        }

        var bajos = diag.Problemas.Where(p => p.Gravedad == Gravedad.Bajo).ToList();
        if (bajos.Count > 0)
        {
            sb.AppendLine("🔵 **AVISOS**");
            foreach (var p in bajos)
                AnalizarProblema(p, diag, sb, recomendaciones);
            sb.AppendLine();
        }

        return Task.FromResult(new LlmResponse(sb.ToString().Trim(), recomendaciones, Name));
    }

    private void AnalizarProblema(Problema p, Diagnostico diag, StringBuilder sb, List<LlmRecomendacion> recs)
    {
        switch (p.Modulo)
        {
            case "Salud" when p.Titulo.Contains("RAM"):
                sb.AppendLine($"  **{p.Titulo}**");
                sb.AppendLine($"  {p.Detalle}");
                if (diag.Salud?.ProcesosPesados.Count > 0)
                {
                    var top = diag.Salud.ProcesosPesados.Take(3);
                    sb.AppendLine($"  Procesos que más consumen: {string.Join(", ", top.Select(x => $"{x.Nombre} ({x.WorkingSetMB} MB)"))}.");
                    sb.AppendLine($"  Sugerencia: cierra las aplicaciones que no uses desde el Administrador de tareas (Ctrl+Shift+Esc).");
                }
                sb.AppendLine();
                break;

            case "Disco":
                sb.AppendLine($"  **{p.Titulo}**");
                sb.AppendLine($"  {p.Detalle}");
                sb.AppendLine($"  Sugerencia: puedes usar la herramienta 'Liberar espacio' de Windows (cleanmgr.exe) o ejecutar la reparación 'Limpiar archivos temporales' desde la app.");
                sb.AppendLine($"  También revisa la carpeta Descargas y Papelera de reciclaje.");
                break;

            case "Windows" when p.Titulo.Contains("Servicios"):
                var svcCount = diag.Windows?.ServiciosFallando.Count ?? 0;
                sb.AppendLine($"  **{p.Titulo}**");
                sb.AppendLine($"  {p.Detalle}");
                if (svcCount > 0)
                {
                    sb.AppendLine($"  Servicios afectados: haz clic en 'Ver servicios' para ver la lista completa e iniciarlos.");
                }
                sb.AppendLine($"  Algunos servicios pueden ser de actualizadores (Edge, Google) y no afectan al funcionamiento diario.");
                break;

            case "Windows" when p.Titulo.Contains("Errores") || p.Titulo.Contains("críticos"):
                sb.AppendLine($"  **{p.Titulo}**");
                sb.AppendLine($"  {p.Detalle}");
                sb.AppendLine($"  Haz clic en 'Abrir Visor de eventos' para ver el detalle de cada error.");
                sb.AppendLine($"  Si los errores son repetitivos, ejecuta la reparación SFC (rep.sfc) para verificar archivos del sistema.");
                break;

            case "Red" when p.Titulo.Contains("Internet"):
                sb.AppendLine($"  **{p.Titulo}**");
                sb.AppendLine($"  {p.Detalle}");
                sb.AppendLine($"  Comprueba el cable de red o WiFi, reinicia el router y ejecuta 'Solucionar problemas' desde el menú.");
                sb.AppendLine($"  También puedes probar: ipconfig /release → ipconfig /renew → ipconfig /flushdns.");
                recs.AddRange(p.ReparacionesSugeridas.Select(id => RepairToRec(id)));
                break;

            case "Red" when p.Titulo.Contains("DNS"):
                sb.AppendLine($"  **{p.Titulo}**");
                sb.AppendLine($"  {p.Detalle}");
                sb.AppendLine($"  Sin DNS no puedes navegar aunque tengas internet. Prueba a usar DNS públicos como 8.8.8.8 (Google) o 1.1.1.1 (Cloudflare).");
                sb.AppendLine($"  Ejecuta la reparación 'Limpiar caché DNS' desde la app.");
                recs.AddRange(p.ReparacionesSugeridas.Select(id => RepairToRec(id)));
                break;

            case "Seguridad" when p.Titulo.Contains("Defender"):
                sb.AppendLine($"  **{p.Titulo}**");
                sb.AppendLine($"  {p.Detalle}");
                sb.AppendLine($"  Sin antivirus activo tu equipo está expuesto a malware. Abre 'Seguridad de Windows' desde el menú de la app para reactivarlo.");
                sb.AppendLine($"  Si tienes otro antivirus instalado (Avast, Norton, etc.), asegúrate de que esté actualizado.");
                break;

            case "Seguridad" when p.Titulo.Contains("Firewall"):
                sb.AppendLine($"  **{p.Titulo}**");
                sb.AppendLine($"  {p.Detalle}");
                sb.AppendLine($"  El firewall bloquea conexiones no autorizadas. Sin él, programas maliciosos pueden comunicarse libremente.");
                sb.AppendLine($"  Abre 'firewall.cpl' desde la app para revisar la configuración.");
                break;

            case "Drivers":
                sb.AppendLine($"  **{p.Titulo}**");
                sb.AppendLine($"  {p.Detalle}");
                sb.AppendLine($"  Los drivers con errores pueden causar pantallazos azules, problemas de sonido, red o vídeo.");
                sb.AppendLine($"  Abre el Administrador de dispositivos desde la app para ver qué dispositivos tienen el icono amarillo.");
                sb.AppendLine($"  Sugerencia: busca las últimas versiones de los drivers en la web del fabricante.");
                break;

            default:
                sb.AppendLine($"  **{p.Titulo}**");
                sb.AppendLine($"  {p.Detalle}");
                if (p.ReparacionesSugeridas.Count > 0)
                    sb.AppendLine($"  Ejecuta la reparación sugerida desde la sección 'Reparaciones' de la app.");
                break;
        }
        sb.AppendLine();
    }

    private static LlmRecomendacion RepairToRec(string id)
    {
        var repair = Repairs.RepairCatalog.Get(id);
        if (repair is not null)
            return new LlmRecomendacion(repair.Titulo, repair.Comando, repair.Descripcion);
        return new LlmRecomendacion(id, "", "");
    }
}
