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
$os = Get-CimInstance Win32_OperatingSystem

$updatePendiente = $false
try {
    $updateSession = New-Object -ComObject Microsoft.Update.Session
    $updateSearcher = $updateSession.CreateUpdateSearcher()
    $searchResult = $updateSearcher.Search("IsInstalled=0 and Type='Software' and IsHidden=0")
    $updatePendiente = $searchResult.Updates.Count -gt 0
} catch {}

$archivosCorruptos = $false
try {
    $dismOut = Dism /Online /Cleanup-Image /CheckHealth
    if ($dismOut -match "reparable|corrupt") { $archivosCorruptos = $true }
} catch {}

$r = [PSCustomObject]@{
    hardware = [PSCustomObject]@{
        cpu = Get-CimInstance Win32_Processor | Select-Object @{N='nombre';E={$_.Name}}, @{N='nucleos';E={$_.NumberOfCores}}, @{N='hilos';E={$_.NumberOfLogicalProcessors}}, @{N='maxFrecuenciaMHz';E={$_.MaxClockSpeed}}
        ram = Get-CimInstance Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum | Select-Object @{N='totalBytes';E={$_.Sum}}
        gpu = @(Get-CimInstance Win32_VideoController | Select-Object @{N='nombre';E={$_.Name}}, @{N='vramBytes';E={$_.AdapterRAM}}, @{N='driverVersion';E={$_.DriverVersion}})
        discos = @(Get-CimInstance Win32_DiskDrive | Select-Object @{N='modelo';E={$_.Model}}, @{N='sizeBytes';E={$_.Size}}, @{N='status';E={$_.Status}})
        discosLogicos = @(Get-CimInstance Win32_LogicalDisk | Where-Object DriveType -eq 3 | Select-Object @{N='letra';E={$_.DeviceID}}, @{N='sizeBytes';E={$_.Size}}, @{N='freeBytes';E={$_.FreeSpace}})
        bios = Get-CimInstance Win32_BIOS | Select-Object @{N='fabricante';E={$_.Manufacturer}}, @{N='version';E={$_.SMBIOSBIOSVersion}}
        placa = Get-CimInstance Win32_BaseBoard | Select-Object @{N='fabricante';E={$_.Manufacturer}}, @{N='producto';E={$_.Product}}
        so = $os | Select-Object @{N='caption';E={$_.Caption}}, @{N='version';E={$_.Version}}, @{N='build';E={$_.BuildNumber}}, @{N='instalado';E={if($_.InstallDate){$_.InstallDate.ToString('o')}}}, @{N='ultimoArranque';E={if($_.LastBootUpTime){$_.LastBootUpTime.ToString('o')}}}
    }
    salud = [PSCustomObject]@{
        diasActivo = [math]::Max(0, [int](Get-Date).Subtract($os.LastBootUpTime).TotalDays)
        horasActivo = [math]::Max(0, [int](Get-Date).Subtract($os.LastBootUpTime).Hours)
        ramLibreMB = [math]::Round($os.FreePhysicalMemory / 1024, 1)
        ramTotalMB = [math]::Round($os.TotalVisibleMemorySize / 1024, 1)
        procesosPesados = @(Get-CimInstance Win32_Process | Sort-Object WorkingSetSize -Descending | Select-Object -First 8 @{N='nombre';E={$_.Name}}, @{N='workingSetMB';E={[math]::Round($_.WorkingSetSize/1MB,1)}}, @{N='pid';E={$_.ProcessId}})
        programasInicio = @(Get-CimInstance Win32_StartupCommand | Select-Object @{N='nombre';E={$_.Name}}, @{N='comando';E={$_.Command}}, @{N='ubicacion';E={$_.Location}})
    }
    windows = [PSCustomObject]@{
        updatePendiente = $updatePendiente
        archivosCorruptos = $archivosCorruptos
        serviciosFallando = @(Get-CimInstance Win32_Service | Where-Object { $_.State -ne 'Running' -and $_.StartMode -eq 'Auto' } | Select-Object @{N='nombre';E={$_.DisplayName}}, @{N='nombreCorto';E={$_.Name}}, @{N='estado';E={$_.State}}, @{N='tipoInicio';E={$_.StartMode}})
        eventosCriticos = @(Get-WinEvent -FilterHashtable @{LogName='System';Level=1,2} -MaxEvents 30 -ErrorAction SilentlyContinue | Select-Object @{N='id';E={$_.Id}}, @{N='nivel';E={$_.Level}}, @{N='fuente';E={$_.ProviderName}}, @{N='mensaje';E={$_.Message}}, @{N='timestamp';E={if($_.TimeCreated){$_.TimeCreated.ToString('o')}}})
    }
    red = [PSCustomObject]@{
        dns = (Get-DnsClientServerAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue | Where-Object { $_.ServerAddresses } | Select-Object -First 1).ServerAddresses[0]
        gateway = (Get-NetRoute -DestinationPrefix '0.0.0.0/0' -ErrorAction SilentlyContinue | Select-Object -First 1).NextHop
        adaptadores = @(Get-NetAdapter -ErrorAction SilentlyContinue | Where-Object Status -eq 'Up' | Select-Object -First 3 @{N='nombre';E={$_.Name}}, @{N='ip';E={(Get-NetIPAddress -InterfaceIndex $_.InterfaceIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue).IPAddress}}, @{N='dhcpActivo';E={$true}}, @{N='tipo';E={$_.MediaType}})
        internet = (Test-Connection 8.8.8.8 -Count 1 -Quiet -ErrorAction SilentlyContinue)
    }
    seguridad = [PSCustomObject]@{
        defenderActivo = (Get-MpComputerStatus -ErrorAction SilentlyContinue).AntivirusEnabled
        firewallActivo = [bool](Get-NetFirewallProfile -ErrorAction SilentlyContinue | Where-Object Name -eq 'Domain').Enabled
        bitlockerActivo = (Get-BitLockerVolume -MountPoint $env:SystemDrive -ErrorAction SilentlyContinue).ProtectionStatus -eq 1
        ultimoAnalisis = [string](Get-MpComputerStatus -ErrorAction SilentlyContinue).LastQuickScanDateTime
    }
    drivers = [PSCustomObject]@{
        dispositivosError = @(Get-CimInstance Win32_PnPEntity -ErrorAction SilentlyContinue | Where-Object { $_.ConfigManagerErrorCode -ne 0 -and $_.ConfigManagerErrorCode -ne 22 -and $_.ConfigManagerErrorCode -ne 24 } | Select-Object -First 10 @{N='nombre';E={$_.Name}}, @{N='codigoError';E={$_.ConfigManagerErrorCode}}, @{N='descripcion';E={$_.Description}})
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

        var readOutputTask = process.StandardOutput.ReadToEndAsync(ct);
        var readErrorTask = process.StandardError.ReadToEndAsync(ct);
        var processExitTask = process.WaitForExitAsync(ct);

        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(90), ct);

        var completedTask = await Task.WhenAny(processExitTask, timeoutTask);
        if (completedTask == timeoutTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignorar errores al matar el proceso
            }
            return null;
        }

        var output = await readOutputTask;
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

        if (raw.Red is not null)
        {
            diag = diag with
            {
                Red = new NetworkInfo
                {
                    DNS = raw.Red.Dns ?? "",
                    Gateway = raw.Red.Gateway ?? "",
                    Internet = raw.Red.Internet,
                    Adaptadores = raw.Red.Adaptadores?.Select(a => new AdaptadorInfo
                    {
                        Nombre = a.Nombre ?? "", IP = a.Ip ?? "",
                        DhcpActivo = a.DhcpActivo, Tipo = a.Tipo ?? ""
                    }).ToList() ?? []
                }
            };
        }

        if (raw.Seguridad is not null)
        {
            diag = diag with
            {
                Seguridad = new SecurityInfo
                {
                    DefenderActivo = raw.Seguridad.DefenderActivo,
                    FirewallActivo = raw.Seguridad.FirewallActivo,
                    BitlockerActivo = raw.Seguridad.BitlockerActivo,
                    UltimoAnalisis = raw.Seguridad.UltimoAnalisis ?? ""
                }
            };
        }

        if (raw.Drivers is not null)
        {
            diag = diag with
            {
                Drivers = new DriverInfo
                {
                    DispositivosError = raw.Drivers.DispositivosError?.Select(d => new DispositivoErrorInfo
                    {
                        Nombre = d.Nombre ?? "",
                        CodigoError = d.CodigoError,
                        Descripcion = d.Descripcion ?? ""
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
        public RedRaw? Red { get; set; }
        public SeguridadRaw? Seguridad { get; set; }
        public DriverRaw? Drivers { get; set; }
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

    private class RedRaw
    {
        public string? Dns { get; set; }
        public string? Gateway { get; set; }
        public List<AdaptadorRaw>? Adaptadores { get; set; }
        public bool Internet { get; set; }
    }
    private class AdaptadorRaw { public string? Nombre { get; set; } public string? Ip { get; set; } public bool DhcpActivo { get; set; } public string? Tipo { get; set; } }

    private class SeguridadRaw
    {
        public bool DefenderActivo { get; set; }
        public bool FirewallActivo { get; set; }
        public bool BitlockerActivo { get; set; }
        public string? UltimoAnalisis { get; set; }
    }

    private class DriverRaw
    {
        public List<DispositivoRaw>? DispositivosError { get; set; }
    }
    private class DispositivoRaw { public string? Nombre { get; set; } public int CodigoError { get; set; } public string? Descripcion { get; set; } }
}
