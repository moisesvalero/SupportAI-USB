namespace SupportAI.Core.Models;

public static class DiagnosticEngine
{
    public static (List<Problema> Problemas, int Puntuacion) Analyze(Diagnostico diag)
    {
        var problemas = new List<Problema>();

        // RAM
        if (diag.Salud?.RamUsoPorcentaje > 90)
        {
            problemas.Add(new Problema
            {
                Gravedad = Gravedad.Alto,
                Modulo = "Salud",
                Titulo = "RAM al límite",
                Detalle = $"La RAM está al {diag.Salud.RamUsoPorcentaje}% de uso. " +
                    (diag.Salud.ProcesosPesados.Count > 0
                        ? $"El proceso {diag.Salud.ProcesosPesados[0].Nombre} consume {diag.Salud.ProcesosPesados[0].WorkingSetMB} MB."
                        : ""),
                ReparacionesSugeridas = []
            });
        }
        else if (diag.Salud?.RamUsoPorcentaje > 75)
        {
            problemas.Add(new Problema
            {
                Gravedad = Gravedad.Medio,
                Modulo = "Salud",
                Titulo = "RAM alta",
                Detalle = $"La RAM está al {diag.Salud.RamUsoPorcentaje}% de uso.",
                ReparacionesSugeridas = []
            });
        }

        // Disco
        if (diag.Hardware?.DiscosLogicos is not null)
        {
            foreach (var d in diag.Hardware.DiscosLogicos)
            {
                if (d.UsoPorcentaje > 95)
                {
                    problemas.Add(new Problema
                    {
                        Gravedad = Gravedad.Critico,
                        Modulo = "Disco",
                        Titulo = $"Disco {d.Letra} sin espacio",
                        Detalle = $"El disco {d.Letra} está al {d.UsoPorcentaje}% de capacidad ({d.FreeGB} GB libres de {d.SizeGB} GB).",
                        ReparacionesSugeridas = ["rep.temp.clean"]
                    });
                }
                else if (d.UsoPorcentaje > 85)
                {
                    problemas.Add(new Problema
                    {
                        Gravedad = Gravedad.Medio,
                        Modulo = "Disco",
                        Titulo = $"Disco {d.Letra} casi lleno",
                        Detalle = $"El disco {d.Letra} está al {d.UsoPorcentaje}% de capacidad ({d.FreeGB} GB libres).",
                        ReparacionesSugeridas = ["rep.temp.clean"]
                    });
                }
            }
        }

        // Uptime
        if (diag.Salud?.DiasActivo > 14)
        {
            problemas.Add(new Problema
            {
                Gravedad = Gravedad.Bajo,
                Modulo = "Salud",
                Titulo = "PC sin reiniciar",
                Detalle = $"El equipo lleva {diag.Salud.DiasActivo} días encendido. Un reinicio puede liberar memoria y aplicar actualizaciones.",
                ReparacionesSugeridas = []
            });
        }

        // Servicios
        if (diag.Windows?.ServiciosFallando is { Count: > 0 })
        {
            var nomServ = string.Join(", ", diag.Windows.ServiciosFallando.Take(3).Select(s => s.NombreCorto));
            problemas.Add(new Problema
            {
                Gravedad = Gravedad.Medio,
                Modulo = "Windows",
                Titulo = "Servicios sin arrancar",
                Detalle = $"{diag.Windows.ServiciosFallando.Count} servicio(s) que deberían estar activos no lo están: {nomServ}{(diag.Windows.ServiciosFallando.Count > 3 ? " ..." : "")}.",
                ReparacionesSugeridas = []
            });
        }

        // Eventos críticos
        var erroresSistema = diag.Windows?.EventosCriticos?.Count ?? 0;
        if (erroresSistema > 5)
        {
            problemas.Add(new Problema
            {
                Gravedad = Gravedad.Medio,
                Modulo = "Windows",
                Titulo = "Errores críticos en el sistema",
                Detalle = $"Se detectaron {erroresSistema} eventos críticos en el visor de eventos del sistema en las últimas sesiones.",
                ReparacionesSugeridas = ["rep.sfc"]
            });
        }

        var puntuacion = CalcularPuntuacion(diag, problemas);
        return (problemas, puntuacion);
    }

    private static int CalcularPuntuacion(Diagnostico diag, List<Problema> problemas)
    {
        var puntos = 100;
        foreach (var p in problemas)
        {
            puntos -= p.Gravedad switch
            {
                Gravedad.Critico => 25,
                Gravedad.Alto => 15,
                Gravedad.Medio => 8,
                Gravedad.Bajo => 3,
                _ => 0
            };
        }
        return Math.Max(0, Math.Min(100, puntos));
    }
}
