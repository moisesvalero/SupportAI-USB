using System.Text.Json.Serialization;

namespace SupportAI.Core.Models;

public record HardwareInfo
{
    [JsonPropertyName("cpu")] public CpuInfo? CPU { get; init; }
    [JsonPropertyName("ram")] public RamInfo? RAM { get; init; }
    [JsonPropertyName("gpu")] public List<GpuInfo> GPU { get; init; } = [];
    [JsonPropertyName("discos")] public List<DiscoInfo> Discos { get; init; } = [];
    [JsonPropertyName("discosLogicos")] public List<DiscoLogicoInfo> DiscosLogicos { get; init; } = [];
    [JsonPropertyName("bios")] public BiosInfo? BIOS { get; init; }
    [JsonPropertyName("placa")] public PlacaInfo? Placa { get; init; }
    [JsonPropertyName("so")] public OsInfo? SO { get; init; }
    [JsonPropertyName("bateria")] public BatteryInfo? Bateria { get; init; }
}

public record CpuInfo
{
    [JsonPropertyName("nombre")] public string Nombre { get; init; } = "";
    [JsonPropertyName("nucleos")] public int Nucleos { get; init; }
    [JsonPropertyName("hilos")] public int Hilos { get; init; }
    [JsonPropertyName("maxFrecuenciaMHz")] public int MaxFrecuenciaMHz { get; init; }
}

public record RamInfo
{
    [JsonPropertyName("totalBytes")] public long TotalBytes { get; init; }
    private const double BytesPerGB = 1_073_741_824.0;
    [JsonPropertyName("totalGB")] public double TotalGB => Math.Round(TotalBytes / BytesPerGB, 1);
}

public record GpuInfo
{
    [JsonPropertyName("nombre")] public string Nombre { get; init; } = "";
    [JsonPropertyName("vramBytes")] public long VRAMBytes { get; init; }
    [JsonPropertyName("driverVersion")] public string DriverVersion { get; init; } = "";
}

public record DiscoInfo
{
    [JsonPropertyName("modelo")] public string Modelo { get; init; } = "";
    [JsonPropertyName("sizeBytes")] public long SizeBytes { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("smartStatus")] public string SmartStatus { get; init; } = "";
}

public record DiscoLogicoInfo
{
    private const double BytesPerGB = 1_073_741_824.0;
    [JsonPropertyName("letra")] public string Letra { get; init; } = "";
    [JsonPropertyName("sizeBytes")] public long SizeBytes { get; init; }
    [JsonPropertyName("freeBytes")] public long FreeBytes { get; init; }
    [JsonPropertyName("sizeGB")] public double SizeGB => Math.Round(SizeBytes / BytesPerGB, 1);
    [JsonPropertyName("freeGB")] public double FreeGB => Math.Round(FreeBytes / BytesPerGB, 1);
    [JsonPropertyName("usoPorcentaje")] public double UsoPorcentaje =>
        SizeBytes > 0 ? Math.Round((1.0 - (double)FreeBytes / SizeBytes) * 100, 1) : 0;
}

public record BiosInfo
{
    [JsonPropertyName("fabricante")] public string Fabricante { get; init; } = "";
    [JsonPropertyName("version")] public string Version { get; init; } = "";
}

public record PlacaInfo
{
    [JsonPropertyName("fabricante")] public string Fabricante { get; init; } = "";
    [JsonPropertyName("producto")] public string Producto { get; init; } = "";
}

public record OsInfo
{
    [JsonPropertyName("caption")] public string Caption { get; init; } = "";
    [JsonPropertyName("version")] public string Version { get; init; } = "";
    [JsonPropertyName("build")] public string Build { get; init; } = "";
    [JsonPropertyName("instalado")] public DateTime? Instalado { get; init; }
    [JsonPropertyName("ultimoArranque")] public DateTime? UltimoArranque { get; init; }
}

public record BatteryInfo
{
    [JsonPropertyName("cargaPorcentaje")] public int CargaPorcentaje { get; init; }
    [JsonPropertyName("desgastePorcentaje")] public int DesgastePorcentaje { get; init; }
    [JsonPropertyName("ciclos")] public int Ciclos { get; init; }
    [JsonPropertyName("tiempoRestanteMin")] public int TiempoRestanteMin { get; init; }
    [JsonPropertyName("conectada")] public bool Conectada { get; init; }
}
