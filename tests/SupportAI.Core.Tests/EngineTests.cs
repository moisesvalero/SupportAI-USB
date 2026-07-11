using SupportAI.Core.Models;

namespace SupportAI.Core.Tests;

public class EngineTests
{
    [Fact]
    public void Analyze_NoProblems_Returns100()
    {
        var diag = new Diagnostico
        {
            Salud = new HealthInfo { RamLibreMB = 8000, RamTotalMB = 16000 },
            Hardware = new HardwareInfo
            {
                DiscosLogicos = [new DiscoLogicoInfo { Letra = "C:", SizeBytes = 256_000_000_000, FreeBytes = 128_000_000_000 }]
            }
        };

        var (prob, score) = DiagnosticEngine.Analyze(diag);

        Assert.Equal(100, score);
        Assert.Empty(prob);
    }

    [Fact]
    public void Analyze_HighRamUsage_ReturnsProblem()
    {
        var diag = new Diagnostico
        {
            Salud = new HealthInfo
            {
                RamLibreMB = 1000,
                RamTotalMB = 16000,
                ProcesosPesados = [new ProcesoInfo { Nombre = "chrome.exe", WorkingSetMB = 3000 }]
            }
        };

        var (prob, score) = DiagnosticEngine.Analyze(diag);

        Assert.Single(prob);
        Assert.Equal(Gravedad.Alto, prob[0].Gravedad);
        Assert.Contains("RAM", prob[0].Titulo);
        Assert.True(score < 100);
    }

    [Fact]
    public void Analyze_CriticalDiskSpace_AddsTempCleanSuggestion()
    {
        var diag = new Diagnostico
        {
            Hardware = new HardwareInfo
            {
                DiscosLogicos = [new DiscoLogicoInfo { Letra = "C:", SizeBytes = 100_000_000_000, FreeBytes = 2_000_000_000 }]
            }
        };

        var (prob, _) = DiagnosticEngine.Analyze(diag);

        Assert.Contains(prob, p => p.ReparacionesSugeridas.Contains("rep.temp.clean"));
    }

    [Fact]
    public void Analyze_FailingServices_DetectsThem()
    {
        var diag = new Diagnostico
        {
            Windows = new WindowsInfo
            {
                ServiciosFallando =
                [
                    new ServicioInfo { Nombre = "Spooler", NombreCorto = "spooler", Estado = "Stopped", TipoInicio = "Auto" }
                ]
            }
        };

        var (prob, _) = DiagnosticEngine.Analyze(diag);

        Assert.Contains(prob, p => p.Modulo == "Windows" && p.Titulo.Contains("Servicios"));
    }

    [Fact]
    public void Analyze_CriticalEvents_DetectsProblems()
    {
        var diag = new Diagnostico
        {
            Windows = new WindowsInfo
            {
                EventosCriticos = Enumerable.Range(0, 10).Select(i =>
                    new EventoInfo { Id = 1000 + i, Nivel = 1, Fuente = "Test", Mensaje = "Error" }
                ).ToList()
            }
        };

        var (prob, _) = DiagnosticEngine.Analyze(diag);

        Assert.Contains(prob, p => p.Titulo.Contains("Errores críticos"));
    }

    [Fact]
    public void Analyze_PcNotRebooted_ReturnsLowSeverity()
    {
        var diag = new Diagnostico
        {
            Salud = new HealthInfo { DiasActivo = 20 }
        };

        var (prob, _) = DiagnosticEngine.Analyze(diag);

        Assert.Contains(prob, p => p.Gravedad == Gravedad.Bajo && p.Titulo.Contains("reiniciar"));
    }

    [Fact]
    public void Analyze_NoInternet_DetectsProblem()
    {
        var diag = new Diagnostico
        {
            Red = new NetworkInfo { Internet = false, DNS = "8.8.8.8" }
        };

        var (prob, _) = DiagnosticEngine.Analyze(diag);

        Assert.Contains(prob, p => p.Modulo == "Red" && p.Titulo.Contains("Internet"));
    }

    [Fact]
    public void Analyze_NoDns_DetectsProblem()
    {
        var diag = new Diagnostico
        {
            Red = new NetworkInfo { Internet = true, DNS = "" }
        };

        var (prob, _) = DiagnosticEngine.Analyze(diag);

        Assert.Contains(prob, p => p.Modulo == "Red" && p.Titulo.Contains("DNS"));
    }

    [Fact]
    public void Analyze_DefenderOff_DetectsProblem()
    {
        var diag = new Diagnostico
        {
            Seguridad = new SecurityInfo { DefenderActivo = false, FirewallActivo = true }
        };

        var (prob, _) = DiagnosticEngine.Analyze(diag);

        Assert.Contains(prob, p => p.Modulo == "Seguridad" && p.Titulo.Contains("Defender"));
    }

    [Fact]
    public void Analyze_FirewallOff_DetectsProblem()
    {
        var diag = new Diagnostico
        {
            Seguridad = new SecurityInfo { DefenderActivo = true, FirewallActivo = false }
        };

        var (prob, _) = DiagnosticEngine.Analyze(diag);

        Assert.Contains(prob, p => p.Modulo == "Seguridad" && p.Titulo.Contains("Firewall"));
    }

    [Fact]
    public void Analyze_DriverErrors_DetectsProblem()
    {
        var diag = new Diagnostico
        {
            Drivers = new DriverInfo
            {
                DispositivosError = [new DispositivoErrorInfo { Nombre = "GPU", CodigoError = 43, Descripcion = "Error" }]
            }
        };

        var (prob, _) = DiagnosticEngine.Analyze(diag);

        Assert.Contains(prob, p => p.Modulo == "Drivers" && p.Titulo.Contains("errores"));
    }

    [Fact]
    public void ServicioInfo_EsCritico_DefaultTrue()
    {
        var svc = new ServicioInfo { Nombre = "Test", NombreCorto = "test" };
        Assert.True(svc.EsCritico);
    }

    [Fact]
    public void ServicioInfo_EsCritico_CanSetFalse()
    {
        var svc = new ServicioInfo { Nombre = "DiagTrack", NombreCorto = "diagtrack", EsCritico = false };
        Assert.False(svc.EsCritico);
    }

    [Fact]
    public void ServicioInfo_EsCritico_NoiseServicesCanBeFlagged()
    {
        var ruidoNames = new[] { "diagtrack", "xblauthmanager", "bthserv", "mapsbroker", "wmpnetworksvc" };
        foreach (var name in ruidoNames)
        {
            var svc = new ServicioInfo { Nombre = name, NombreCorto = name, EsCritico = false };
            Assert.False(svc.EsCritico, $"{name} should be non-critical");
        }
    }

    [Fact]
    public void Analyze_HealthyNetwork_NoProblems()
    {
        var diag = new Diagnostico
        {
            Red = new NetworkInfo { Internet = true, DNS = "1.1.1.1" },
            Seguridad = new SecurityInfo { DefenderActivo = true, FirewallActivo = true },
            Drivers = new DriverInfo { DispositivosError = [] },
            Salud = new HealthInfo { RamLibreMB = 8000, RamTotalMB = 16000 },
            Hardware = new HardwareInfo
            {
                DiscosLogicos = [new DiscoLogicoInfo { Letra = "C:", SizeBytes = 256_000_000_000, FreeBytes = 128_000_000_000 }]
            }
        };

        var (prob, _) = DiagnosticEngine.Analyze(diag);

        Assert.DoesNotContain(prob, p => p.Modulo == "Red");
        Assert.DoesNotContain(prob, p => p.Modulo == "Seguridad");
        Assert.DoesNotContain(prob, p => p.Modulo == "Drivers");
    }
}
