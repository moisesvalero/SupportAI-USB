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
                ReparacionesSugeridas = [],
                AccionLabel = "Ver procesos",
                AccionTarget = "taskmgr"
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
                ReparacionesSugeridas = [],
                AccionLabel = "Ver procesos",
                AccionTarget = "taskmgr"
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
                        ReparacionesSugeridas = ["rep.temp.clean"],
                        AccionLabel = "Limpiar disco",
                        AccionTarget = "cleanmgr"
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
                        ReparacionesSugeridas = ["rep.temp.clean"],
                        AccionLabel = "Limpiar disco",
                        AccionTarget = "cleanmgr"
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
                ReparacionesSugeridas = [],
                AccionLabel = "Reiniciar ahora",
                AccionTarget = "shutdown"
            });
        }

        // Servicios
        if (diag.Windows?.ServiciosFallando is { Count: > 0 })
        {
            var count = diag.Windows.ServiciosFallando.Count;
            var nomServ = string.Join(", ", diag.Windows.ServiciosFallando.Take(3).Select(s => s.NombreCorto));
            problemas.Add(new Problema
            {
                Gravedad = Gravedad.Medio,
                Modulo = "Windows",
                Titulo = "Servicios sin arrancar",
                Detalle = $"{count} servicio(s) que deberían estar activos no lo están: {nomServ}{(count > 3 ? " ..." : "")}.",
                ReparacionesSugeridas = [],
                AccionLabel = "Ver servicios",
                AccionTarget = "expand:servicios"
            });
        }

        // Red
        if (diag.Red is not null)
        {
            if (!diag.Red.Internet)
            {
                problemas.Add(new Problema
                {
                    Gravedad = Gravedad.Alto,
                    Modulo = "Red",
                    Titulo = "Sin conexión a Internet",
                    Detalle = "No se pudo contactar con 8.8.8.8. Puede deberse a un problema de red, DNS o firewall.",
                    ReparacionesSugeridas = ["rep.dns.flush", "rep.winsock.reset"],
                    AccionLabel = "Solucionar problemas",
                    AccionTarget = "ms-settings:network-troubleshoot"
                });
            }
            if (string.IsNullOrWhiteSpace(diag.Red.DNS))
            {
                problemas.Add(new Problema
                {
                    Gravedad = Gravedad.Medio,
                    Modulo = "Red",
                    Titulo = "DNS no configurado",
                    Detalle = "No se detectaron servidores DNS configurados en ninguna interfaz de red activa.",
                    ReparacionesSugeridas = ["rep.dns.flush"],
                    AccionLabel = "Configuración de red",
                    AccionTarget = "ms-settings:network-status"
                });
            }
        }

        // Seguridad
        if (diag.Seguridad is not null)
        {
            if (!diag.Seguridad.DefenderActivo)
            {
                problemas.Add(new Problema
                {
                    Gravedad = Gravedad.Alto,
                    Modulo = "Seguridad",
                    Titulo = "Defender desactivado",
                    Detalle = "Microsoft Defender no está activo. El equipo podría estar desprotegido.",
                    ReparacionesSugeridas = [],
                    AccionLabel = "Abrir Seguridad",
                    AccionTarget = "windowsdefender:"
                });
            }
            if (!diag.Seguridad.FirewallActivo)
            {
                problemas.Add(new Problema
                {
                    Gravedad = Gravedad.Alto,
                    Modulo = "Seguridad",
                    Titulo = "Firewall desactivado",
                    Detalle = "El firewall de Windows no está activo en el perfil Domain. Riesgo de seguridad.",
                    ReparacionesSugeridas = [],
                    AccionLabel = "Abrir firewall",
                    AccionTarget = "firewall.cpl"
                });
            }
        }

        // Drivers
        if (diag.Drivers?.DispositivosError is { Count: > 0 })
        {
            var count = diag.Drivers.DispositivosError.Count;
            var nomDrv = string.Join(", ", diag.Drivers.DispositivosError.Take(3).Select(d => d.Nombre));
            problemas.Add(new Problema
            {
                Gravedad = Gravedad.Medio,
                Modulo = "Drivers",
                Titulo = "Dispositivos con errores",
                Detalle = $"{count} dispositivo(s) tienen errores de driver: {nomDrv}{(count > 3 ? " ..." : "")}.",
                ReparacionesSugeridas = [],
                AccionLabel = "Administrador de dispositivos",
                AccionTarget = "devmgmt.msc"
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
                ReparacionesSugeridas = ["rep.sfc"],
                AccionLabel = "Abrir Visor de eventos",
                AccionTarget = "eventvwr.msc"
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
