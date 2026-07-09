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
}
