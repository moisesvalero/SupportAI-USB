using System.Text.Json.Serialization;

namespace SupportAI.Core.Models;

public record HealthInfo
{
    [JsonPropertyName("diasActivo")] public int DiasActivo { get; init; }
    [JsonPropertyName("horasActivo")] public int HorasActivo { get; init; }
    [JsonPropertyName("ramLibreMB")] public double RamLibreMB { get; init; }
    [JsonPropertyName("ramTotalMB")] public double RamTotalMB { get; init; }
    [JsonPropertyName("ramUsoPorcentaje")] public double RamUsoPorcentaje =>
        RamTotalMB > 0 ? Math.Round((1.0 - RamLibreMB / RamTotalMB) * 100, 1) : 0;
    [JsonPropertyName("procesosPesados")] public List<ProcesoInfo> ProcesosPesados { get; init; } = [];
    [JsonPropertyName("programasInicio")] public List<StartupInfo> ProgramasInicio { get; init; } = [];
    [JsonPropertyName("cpuUsoPorcentaje")] public double CpuUsoPorcentaje { get; init; }
    [JsonPropertyName("cpuTemperatura")] public double CpuTemperatura { get; init; }
    [JsonPropertyName("frecuenciaActualMHz")] public int FrecuenciaActualMHz { get; init; }
    [JsonPropertyName("cpuThrottling")] public bool CpuThrottling { get; init; }
    [JsonPropertyName("planEnergia")] public string PlanEnergia { get; init; } = "";
    [JsonPropertyName("pageFileTotalMB")] public double PageFileTotalMB { get; init; }
    [JsonPropertyName("pageFileUsadoMB")] public double PageFileUsadoMB { get; init; }
}

public record ProcesoInfo
{
    [JsonPropertyName("nombre")] public string Nombre { get; init; } = "";
    [JsonPropertyName("workingSetMB")] public double WorkingSetMB { get; init; }
    [JsonPropertyName("pid")] public int PID { get; init; }
}

public record StartupInfo
{
    [JsonPropertyName("nombre")] public string Nombre { get; init; } = "";
    [JsonPropertyName("comando")] public string Comando { get; init; } = "";
    [JsonPropertyName("ubicacion")] public string Ubicacion { get; init; } = "";
}
