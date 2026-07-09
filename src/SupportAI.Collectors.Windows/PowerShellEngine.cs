using System.Diagnostics;
using System.Text.Json;
using SupportAI.Core.Models;

namespace SupportAI.Collectors.Windows;

public class PowerShellEngine
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    public async Task<Diagnostico> CollectAllAsync(CancellationToken ct)
    {
        var script = BuildFullScript();
        var json = await RunPowerShellAsync(script, ct);
        if (string.IsNullOrWhiteSpace(json))
            return new Diagnostico { GeneradoEn = DateTime.UtcNow };

        var diag = JsonSerializer.Deserialize<DiagnosticoRaw>(json, JsonOpts);
        return MapToDiagnostico(diag);
    }

    private static string BuildFullScript()
    {
        return """
$ErrorActionPreference = 'SilentlyContinue'
$r = [PSCustomObject]@{
    hardware = [PSCustomObject]@{
        cpu = Get-CimInstance Win32_Processor | Select-Object @{N='nombre';E={$_.Name}}, @{N='nucleos';E={$_.NumberOfCores}}, @{N='hilos';E={$_.NumberOfLogicalProcessors}}, @{N='maxFrecuenciaMHz';E={$_.MaxClockSpeed}}
        ram = Get-CimInstance Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum | Select-Object @{N='totalBytes';E={$_.Sum}}
        gpu = Get-CimInstance Win32_VideoController | Select-Object @{N='nombre';E={$_.Name}}, @{N='vramBytes';E={$_.AdapterRAM}}, @{N='driverVersion';E={$_.DriverVersion}}
        discos = Get-CimInstance Win32_DiskDrive | Select-Object @{N='modelo';E={$_.Model}}, @{N='sizeBytes';E={$_.Size}}, @{N='status';E={$_.Status}}
        discosLogicos = Get-CimInstance Win32_LogicalDisk | Where-Object DriveType -eq 3 | Select-Object @{N='letra';E={$_.DeviceID}}, @{N='sizeBytes';E={$_.Size}}, @{N='freeBytes';E={$_.FreeSpace}}
        bios = Get-CimInstance Win32_BIOS | Select-Object @{N='fabricante';E={$_.Manufacturer}}, @{N='version';E={$_.SMBIOSBIOSVersion}}
        placa = Get-CimInstance Win32_BaseBoard | Select-Object @{N='fabricante';E={$_.Manufacturer}}, @{N='producto';E={$_.Product}}
        so = Get-CimInstance Win32_OperatingSystem | Select-Object @{N='caption';E={$_.Caption}}, @{N='version';E={$_.Version}}, @{N='build';E={$_.BuildNumber}}, @{N='instalado';E={$_.InstallDate}}, @{N='ultimoArranque';E={$_.LastBootUpTime}}
    }
    salud = [PSCustomObject]@{
        diasActivo = if ($_.UltimoArranque) { [math]::Max(0, [int](Get-Date).Subtract((Get-CimInstance Win32_OperatingSystem).LastBootUpTime).TotalDays) } else { 0 }
        horasActivo = if ($_.UltimoArranque) { [math]::Max(0, [int](Get-Date).Subtract((Get-CimInstance Win32_OperatingSystem).LastBootUpTime).Hours) } else { 0 }
        ramLibreMB = [math]::Round((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1024, 1)
        ramTotalMB = [math]::Round((Get-CimInstance Win32_OperatingSystem).TotalVisibleMemorySize / 1024, 1)
        procesosPesados = Get-CimInstance Win32_Process | Sort-Object WorkingSetSize -Descending | Select-Object -First 8 @{N='nombre';E={$_.Name}}, @{N='workingSetMB';E={[math]::Round($_.WorkingSetSize/1MB,1)}}, @{N='pid';E={$_.ProcessId}}
        programasInicio = Get-CimInstance Win32_StartupCommand | Select-Object @{N='nombre';E={$_.Name}}, @{N='comando';E={$_.Command}}, @{N='ubicacion';E={$_.Location}}
    }
    windows = [PSCustomObject]@{
        updatePendiente = $false
        archivosCorruptos = $false
        serviciosFallando = Get-CimInstance Win32_Service | Where-Object { $_.State -ne 'Running' -and $_.StartMode -eq 'Auto' } | Select-Object @{N='nombre';E={$_.DisplayName}}, @{N='nombreCorto';E={$_.Name}}, @{N='estado';E={$_.State}}, @{N='tipoInicio';E={$_.StartMode}}
        eventosCriticos = Get-WinEvent -FilterHashtable @{LogName='System';Level=1,2} -MaxEvents 30 -ErrorAction SilentlyContinue | Select-Object @{N='id';E={$_.Id}}, @{N='nivel';E={$_.Level}}, @{N='fuente';E={$_.ProviderName}}, @{N='mensaje';E={$_.Message}}, @{N='timestamp';E={$_.TimeCreated}}
    }
}
$r | ConvertTo-Json -Depth 5
""";
    }

    private static async Task<string?> RunPowerShellAsync(string script, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return string.IsNullOrEmpty(output) ? null : output.Trim();
    }

    private static Diagnostico MapToDiagnostico(DiagnosticoRaw? raw)
    {
        if (raw is null) return new Diagnostico { GeneradoEn = DateTime.UtcNow };
        var diag = new Diagnostico { GeneradoEn = DateTime.UtcNow };

        if (raw.Hardware is not null)
        {
            diag = diag with
            {
                Hardware = new HardwareInfo
                {
                    CPU = raw.Hardware.Cpu is { } c ? new CpuInfo
                    {
                        Nombre = c.Nombre ?? "",
                        Nucleos = c.Nucleos,
                        Hilos = c.Hilos,
                        MaxFrecuenciaMHz = c.MaxFrecuenciaMHz
                    } : null,
                    RAM = raw.Hardware.Ram is { } r ? new RamInfo { TotalBytes = r.TotalBytes } : null,
                    GPU = raw.Hardware.Gpu?.Select(g => new GpuInfo
                    {
                        Nombre = g.Nombre ?? "", VRAMBytes = g.VramBytes, DriverVersion = g.DriverVersion ?? ""
                    }).ToList() ?? [],
                    Discos = raw.Hardware.Discos?.Select(d => new DiscoInfo
                    {
                        Modelo = d.Modelo ?? "", SizeBytes = d.SizeBytes, Status = d.Status ?? ""
                    }).ToList() ?? [],
                    DiscosLogicos = raw.Hardware.DiscosLogicos?.Select(d => new DiscoLogicoInfo
                    {
                        Letra = d.Letra ?? "", SizeBytes = d.SizeBytes, FreeBytes = d.FreeBytes
                    }).ToList() ?? [],
                    BIOS = raw.Hardware.Bios is { } b ? new BiosInfo
                    {
                        Fabricante = b.Fabricante ?? "", Version = b.Version ?? ""
                    } : null,
                    Placa = raw.Hardware.Placa is { } p ? new PlacaInfo
                    {
                        Fabricante = p.Fabricante ?? "", Producto = p.Producto ?? ""
                    } : null,
                    SO = raw.Hardware.So is { } s ? new OsInfo
                    {
                        Caption = s.Caption ?? "", Version = s.Version ?? "", Build = s.Build ?? ""
                    } : null
                }
            };
        }

        if (raw.Salud is not null)
        {
            diag = diag with
            {
                Salud = new HealthInfo
                {
                    DiasActivo = raw.Salud.DiasActivo,
                    HorasActivo = raw.Salud.HorasActivo,
                    RamLibreMB = raw.Salud.RamLibreMB,
                    RamTotalMB = raw.Salud.RamTotalMB,
                    ProcesosPesados = raw.Salud.ProcesosPesados?.Select(p => new ProcesoInfo
                    {
                        Nombre = p.Nombre ?? "", WorkingSetMB = p.WorkingSetMB, PID = p.PID
                    }).ToList() ?? [],
                    ProgramasInicio = raw.Salud.ProgramasInicio?.Select(s => new StartupInfo
                    {
                        Nombre = s.Nombre ?? "", Comando = s.Comando ?? "", Ubicacion = s.Ubicacion ?? ""
                    }).ToList() ?? []
                }
            };
        }

        if (raw.Windows is not null)
        {
            diag = diag with
            {
                Windows = new WindowsInfo
                {
                    UpdatePendiente = raw.Windows.UpdatePendiente,
                    ArchivosCorruptos = raw.Windows.ArchivosCorruptos,
                    ServiciosFallando = raw.Windows.ServiciosFallando?.Select(s => new ServicioInfo
                    {
                        Nombre = s.Nombre ?? "", NombreCorto = s.NombreCorto ?? "",
                        Estado = s.Estado ?? "", TipoInicio = s.TipoInicio ?? ""
                    }).ToList() ?? [],
                    EventosCriticos = raw.Windows.EventosCriticos?.Select(e => new EventoInfo
                    {
                        Id = e.Id, Nivel = e.Nivel, Fuente = e.Fuente ?? "",
                        Mensaje = e.Mensaje ?? "", Timestamp = e.Timestamp
                    }).ToList() ?? []
                }
            };
        }

        return diag;
    }

    // Raw DTOs for JSON deserialization
    private class DiagnosticoRaw
    {
        public HardwareRaw? Hardware { get; set; }
        public SaludRaw? Salud { get; set; }
        public WindowsRaw? Windows { get; set; }
    }

    private class HardwareRaw
    {
        public CpuRaw? Cpu { get; set; }
        public RamRaw? Ram { get; set; }
        public List<GpuRaw>? Gpu { get; set; }
        public List<DiscoRaw>? Discos { get; set; }
        public List<DiscoLogicoRaw>? DiscosLogicos { get; set; }
        public BiosRaw? Bios { get; set; }
        public PlacaRaw? Placa { get; set; }
        public OsRaw? So { get; set; }
    }

    private class CpuRaw { public string? Nombre { get; set; } public int Nucleos { get; set; } public int Hilos { get; set; } public int MaxFrecuenciaMHz { get; set; } }
    private class RamRaw { public long TotalBytes { get; set; } }
    private class GpuRaw { public string? Nombre { get; set; } public long VramBytes { get; set; } public string? DriverVersion { get; set; } }
    private class DiscoRaw { public string? Modelo { get; set; } public long SizeBytes { get; set; } public string? Status { get; set; } }
    private class DiscoLogicoRaw { public string? Letra { get; set; } public long SizeBytes { get; set; } public long FreeBytes { get; set; } }
    private class BiosRaw { public string? Fabricante { get; set; } public string? Version { get; set; } }
    private class PlacaRaw { public string? Fabricante { get; set; } public string? Producto { get; set; } }
    private class OsRaw { public string? Caption { get; set; } public string? Version { get; set; } public string? Build { get; set; } public DateTime? Instalado { get; set; } public DateTime? UltimoArranque { get; set; } }

    private class SaludRaw
    {
        public int DiasActivo { get; set; }
        public int HorasActivo { get; set; }
        public double RamLibreMB { get; set; }
        public double RamTotalMB { get; set; }
        public List<ProcesoRaw>? ProcesosPesados { get; set; }
        public List<StartupRaw>? ProgramasInicio { get; set; }
    }

    private class ProcesoRaw { public string? Nombre { get; set; } public double WorkingSetMB { get; set; } public int PID { get; set; } }
    private class StartupRaw { public string? Nombre { get; set; } public string? Comando { get; set; } public string? Ubicacion { get; set; } }

    private class WindowsRaw
    {
        public bool UpdatePendiente { get; set; }
        public bool ArchivosCorruptos { get; set; }
        public List<ServicioRaw>? ServiciosFallando { get; set; }
        public List<EventoRaw>? EventosCriticos { get; set; }
    }

    private class ServicioRaw { public string? Nombre { get; set; } public string? NombreCorto { get; set; } public string? Estado { get; set; } public string? TipoInicio { get; set; } }
    private class EventoRaw { public int Id { get; set; } public int Nivel { get; set; } public string? Fuente { get; set; } public string? Mensaje { get; set; } public DateTime? Timestamp { get; set; } }
}
