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
}
