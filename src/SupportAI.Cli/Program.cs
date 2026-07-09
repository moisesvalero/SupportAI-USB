using System.Text.Json;
using SupportAI.Collectors.Windows;
using SupportAI.Core.Models;
using SupportAI.Repairs;

var jsonOpts = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("""
SupportAI USB - Asistente de diagnóstico para Windows
Uso:
  --diagnose          Ejecutar diagnóstico completo
  --json              Salida en JSON (por defecto con --diagnose)
  --out <archivo>     Guardar salida en archivo
  --repair <id>       Ejecutar reparación (usa --dry-run para simular)
  --list-repairs      Listar reparaciones disponibles
  --dry-run           Simular sin aplicar cambios
  --help              Esta ayuda
""");
    return;
}

if (args.Contains("--list-repairs"))
{
    Console.WriteLine("Reparaciones disponibles:");
    foreach (var r in RepairCatalog.All)
        Console.WriteLine($"  {r.Id,-25} {r.Titulo}");
    return;
}

if (args.Contains("--repair"))
{
    var idx = Array.IndexOf(args, "--repair") + 1;
    if (idx >= args.Length) { Console.Error.WriteLine("Falta ID de reparación."); return; }
    var id = args[idx];
    var repair = RepairCatalog.Get(id);
    if (repair is null) { Console.Error.WriteLine($"Reparación '{id}' no encontrada."); return; }

    var dryRun = args.Contains("--dry-run");
    Console.WriteLine($"{(dryRun ? "[SIMULACIÓN] " : "")}Ejecutando: {repair.Titulo}");
    var result = await repair.ExecuteAsync(dryRun);
    Console.WriteLine(result.Success ? "OK" : "ERROR");
    Console.WriteLine(result.Output);
    if (!string.IsNullOrEmpty(result.Error))
        Console.Error.WriteLine(result.Error);
    return;
}

// Default: diagnose
Console.WriteLine("SupportAI USB - Diagnosticando...");

var engine = new PowerShellEngine();
var diag = await engine.CollectAllAsync(CancellationToken.None);

var (problemas, puntuacion) = DiagnosticEngine.Analyze(diag);
diag = diag with { Problemas = problemas, Puntuacion = puntuacion };

if (args.Contains("--json") || args.Length == 0)
{
    var json = JsonSerializer.Serialize(diag, jsonOpts);
    var outFile = GetOutPath(args);
    if (outFile is not null)
    {
        await File.WriteAllTextAsync(outFile, json);
        Console.WriteLine($"Diagnóstico guardado en: {outFile}");
    }
    else
    {
        Console.WriteLine(json);
    }
}
else
{
    PrintReport(diag);
}

static string? GetOutPath(string[] args)
{
    var idx = Array.IndexOf(args, "--out") + 1;
    return idx > 0 && idx < args.Length ? args[idx] : null;
}

void PrintReport(Diagnostico diag)
{
    Console.WriteLine($"\n=== SUPPORTAI USB - DIAGNÓSTICO ===");
    Console.WriteLine($"Generado: {diag.GeneradoEn:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"Puntuación: {diag.Puntuacion}/100\n");

    if (diag.Hardware?.SO is { } os)
        Console.WriteLine($"Sistema: {os.Caption} {os.Version} (build {os.Build})");
    if (diag.Hardware?.CPU is { } cpu)
        Console.WriteLine($"CPU: {cpu.Nombre} ({cpu.Nucleos} núcleos, {cpu.Hilos} hilos)");
    if (diag.Hardware?.RAM is { } ram)
        Console.WriteLine($"RAM: {ram.TotalGB} GB");
    if (diag.Salud is { } salud)
        Console.WriteLine($"RAM en uso: {salud.RamUsoPorcentaje}% | Activo: {salud.DiasActivo}d {salud.HorasActivo}h");
    if (diag.Hardware?.DiscosLogicos is { } discos)
        foreach (var d in discos)
            Console.WriteLine($"Disco {d.Letra}: {d.FreeGB}/{d.SizeGB} GB ({d.UsoPorcentaje}% usado)");

    Console.WriteLine($"\n--- Problemas ({problemas.Count}) ---");
    foreach (var p in problemas)
    {
        var icono = p.Gravedad switch
        {
            Gravedad.Critico => "🚨", Gravedad.Alto => "🔴",
            Gravedad.Medio => "⚠️", Gravedad.Bajo => "🔵", _ => "•"
        };
        Console.WriteLine($"\n{icono} [{p.Gravedad}] {p.Titulo}");
        Console.WriteLine($"   {p.Detalle}");
        if (p.ReparacionesSugeridas.Count > 0)
            Console.WriteLine($"   Sugerencia: SupportAI.exe --repair {p.ReparacionesSugeridas[0]}");
    }

    Console.WriteLine($"\nPara ver y ejecutar reparaciones:");
    Console.WriteLine("  SupportAI.exe --list-repairs");
    Console.WriteLine("  SupportAI.exe --repair rep.temp.clean");
    Console.WriteLine("\nPara salida JSON:");
    Console.WriteLine("  SupportAI.exe --diagnose --json --out diagnostico.json");
}
