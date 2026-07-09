using SupportAI.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SupportAI.App.Wpf.Services;

public static class InformeService
{
    public static async Task GenerateAsync(Diagnostico diag, string path)
    {
        await Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Element(c => ComposeHeader(c, diag));
                    page.Content().Element(c => ComposeContent(c, diag));
                    page.Footer().AlignCenter().Text($"Generado por SupportAI USB — {diag.GeneradoEn:dd/MM/yyyy HH:mm}");
                });
            }).GeneratePdf(path);
        });
    }

    private static void ComposeHeader(IContainer container, Diagnostico diag)
    {
        container.Column(col =>
        {
            col.Item().Text("SupportAI USB — Informe de diagnóstico")
                .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
            col.Item().Text($"Puntuación: {diag.Puntuacion}/100")
                .FontSize(14);
            col.Item().Height(10);
        });
    }

    private static void ComposeContent(IContainer container, Diagnostico diag)
    {
        container.Column(col =>
        {
            if (diag.Hardware?.SO is { } os)
                col.Item().Text($"Sistema: {os.Caption} {os.Version} (build {os.Build})");
            if (diag.Hardware?.CPU is { } cpu)
                col.Item().Text($"CPU: {cpu.Nombre} — {cpu.Nucleos} núcleos/{cpu.Hilos} hilos");
            if (diag.Hardware?.RAM is { } ram)
                col.Item().Text($"RAM: {ram.TotalGB} GB");
            if (diag.Salud is { } salud)
                col.Item().Text($"RAM en uso: {salud.RamUsoPorcentaje}% | Activo: {salud.DiasActivo}d");

            col.Item().Height(15);

            if (diag.Problemas.Count > 0)
            {
                col.Item().Text("Problemas detectados:").Bold().FontSize(12);
                foreach (var p in diag.Problemas)
                {
                    col.Item().PaddingLeft(10).Text($"• [{p.Gravedad}] {p.Titulo}")
                        .FontColor(p.Gravedad switch
                        {
                            Gravedad.Critico or Gravedad.Alto => Colors.Red.Medium,
                            Gravedad.Medio => Colors.Orange.Medium,
                            _ => Colors.Blue.Medium
                        });
                    col.Item().PaddingLeft(20).Text(p.Detalle).FontSize(9);
                }
            }
            else
            {
                col.Item().Text("No se detectaron problemas.").FontColor(Colors.Green.Medium);
            }

            col.Item().Height(15);
            col.Item().Text("Recomendaciones:").Bold().FontSize(12);
            col.Item().PaddingLeft(10).Text("Revisar los problemas detectados y aplicar las reparaciones sugeridas.");
            col.Item().PaddingLeft(10).Text("Si los síntomas persisten, contactar con soporte técnico.");
        });
    }
}
