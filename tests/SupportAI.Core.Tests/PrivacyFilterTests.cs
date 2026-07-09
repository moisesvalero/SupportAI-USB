using SupportAI.Core.Models;
using SupportAI.Ia;

namespace SupportAI.Core.Tests;

public class PrivacyFilterTests
{
    [Fact]
    public void Anonymize_ReplacesSerialNumbers()
    {
        var diag = new Diagnostico
        {
            Hardware = new HardwareInfo
            {
                BIOS = new BiosInfo { Fabricante = "Dell Inc.", Version = "2.3.0 ABC12345" }
            }
        };

        var result = PrivacyFilter.Anonymize(diag);

        Assert.Contains("[REDACTED]", result.Hardware?.BIOS?.Version);
        Assert.DoesNotContain("ABC12345", result.Hardware?.BIOS?.Version);
    }

    [Fact]
    public void Anonymize_ReplacesIpAddresses()
    {
        var diag = new Diagnostico
        {
            Red = new NetworkInfo
            {
                DNS = "8.8.8.8",
                Adaptadores = [new AdaptadorInfo { IP = "192.168.1.10", Nombre = "Ethernet" }]
            }
        };

        var result = PrivacyFilter.Anonymize(diag);

        Assert.Equal("[DNS_SERVER]", result.Red?.DNS);
        Assert.Equal("[IP_ADDRESS]", result.Red?.Adaptadores[0].IP);
    }

    [Fact]
    public void Anonymize_ReplacesUsername()
    {
        var diag = new Diagnostico
        {
            Hardware = new HardwareInfo
            {
                SO = new OsInfo { Caption = Environment.UserName + " PC", Version = "10.0.22621" }
            }
        };

        var result = PrivacyFilter.Anonymize(diag);

        Assert.Contains("[USER]", result.Hardware?.SO?.Caption);
    }

    [Fact]
    public void Anonymize_LeavesCleanData()
    {
        var diag = new Diagnostico
        {
            Hardware = new HardwareInfo
            {
                RAM = new RamInfo { TotalBytes = 17179869184 },
                CPU = new CpuInfo { Nombre = "Intel i7", Nucleos = 8 }
            }
        };

        var result = PrivacyFilter.Anonymize(diag);

        Assert.Equal(17179869184, result.Hardware?.RAM?.TotalBytes);
        Assert.Equal("Intel i7", result.Hardware?.CPU?.Nombre);
    }

    [Fact]
    public void Anonymize_ReplacesPiiInStartupAndEvents()
    {
        var username = Environment.UserName;
        var machineName = Environment.MachineName;
        var domainName = Environment.UserDomainName;

        var diag = new Diagnostico
        {
            Salud = new HealthInfo
            {
                ProgramasInicio =
                [
                    new StartupInfo
                    {
                        Nombre = "MaliciousApp",
                        Comando = $@"C:\Users\{username}\AppData\Local\Temp\malicious.exe --user={username}",
                        Ubicacion = $@"HKCU\Software\Microsoft\Windows\CurrentVersion\Run"
                    }
                ]
            },
            Windows = new WindowsInfo
            {
                EventosCriticos =
                [
                    new EventoInfo
                    {
                        Fuente = "DCOM",
                        Mensaje = $"El servidor no pudo registrarse en DCOM con el equipo {machineName} de {domainName}"
                    }
                ]
            }
        };

        var result = PrivacyFilter.Anonymize(diag);

        // Verificaciones
        Assert.NotNull(result.Salud);
        Assert.Single(result.Salud.ProgramasInicio);
        var startup = result.Salud.ProgramasInicio[0];
        Assert.Contains(@"C:\Users\[USER]\AppData\Local\Temp\malicious.exe", startup.Comando);
        Assert.DoesNotContain(username, startup.Comando, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(result.Windows);
        Assert.Single(result.Windows.EventosCriticos);
        var ev = result.Windows.EventosCriticos[0];
        Assert.Contains("[COMPUTER]", ev.Mensaje);
        Assert.DoesNotContain(machineName, ev.Mensaje, StringComparison.OrdinalIgnoreCase);
        
        if (!string.IsNullOrEmpty(domainName) && 
            !domainName.Equals(machineName, StringComparison.OrdinalIgnoreCase) && 
            !domainName.Equals("WORKGROUP", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("[DOMAIN]", ev.Mensaje);
            Assert.DoesNotContain(domainName, ev.Mensaje, StringComparison.OrdinalIgnoreCase);
        }
    }
}
