using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SupportAI.Collectors.Windows;
using SupportAI.Core.Models;
using SupportAI.Repairs;

namespace SupportAI.App.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly PowerShellEngine _engine = new();
    private Diagnostico _diagnostico = new();

    public MainViewModel()
    {
        ScanCommand = new AsyncRelayCommand(async _ => await ScanAsync());
        ExportPdfCommand = new AsyncRelayCommand(async _ => await ExportPdfAsync());
        RepairCommand = new RelayCommand(ExecuteRepair);
        StatusText = "Listo. Haz clic en [Escanear] para diagnosticar.";
    }

    public ICommand ScanCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand RepairCommand { get; }

    private string _statusText = "";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    private int _puntuacion;
    public int Puntuacion
    {
        get => _puntuacion;
        set { _puntuacion = value; OnPropertyChanged(); OnPropertyChanged(nameof(PuntuacionColor)); }
    }

    public string PuntuacionColor => Puntuacion switch
    {
        >= 80 => "#27ae60",
        >= 50 => "#f39c12",
        _ => "#e74c3c"
    };

    public string Sistema => _diagnostico.Hardware?.SO?.Caption ?? "";
    public string CpuInfo => _diagnostico.Hardware?.CPU is { } c ? $"{c.Nombre} ({c.Nucleos} núcleos)" : "";
    public string RamInfo => _diagnostico.Hardware?.RAM is { } r ? $"{r.TotalGB} GB" : "";
    public string RamUso => _diagnostico.Salud is { } s ? $"{s.RamUsoPorcentaje}%" : "";
    public string Uptime => _diagnostico.Salud is { } s ? $"{s.DiasActivo}d {s.HorasActivo}h" : "";

    public List<ModuloCheck> Modulos { get; set; } = [];
    public List<Problema> Problemas { get; set; } = [];
    public List<IRepairAction> Repairs => RepairCatalog.All.ToList();
    public bool HayProblemas => Problemas.Count > 0;
    public bool TieneDatos => Puntuacion > 0;

    public async Task ScanAsync()
    {
        StatusText = "Escaneando...";
        Puntuacion = 0;

        _diagnostico = await _engine.CollectAllAsync(CancellationToken.None);
        var (problemas, puntuacion) = DiagnosticEngine.Analyze(_diagnostico);
        _diagnostico = _diagnostico with { Problemas = problemas, Puntuacion = puntuacion };

        Puntuacion = puntuacion;
        Problemas = problemas;
        Modulos = BuildModulos(_diagnostico);

        OnPropertyChanged(nameof(Sistema));
        OnPropertyChanged(nameof(CpuInfo));
        OnPropertyChanged(nameof(RamInfo));
        OnPropertyChanged(nameof(RamUso));
        OnPropertyChanged(nameof(Uptime));
        OnPropertyChanged(nameof(Modulos));
        OnPropertyChanged(nameof(Problemas));
        OnPropertyChanged(nameof(HayProblemas));
        OnPropertyChanged(nameof(TieneDatos));

        StatusText = $"Diagnóstico completado. Puntuación: {Puntuacion}/100. {Problemas.Count} problema(s).";
    }

    private void ExecuteRepair(object? parameter)
    {
        if (parameter is not string id) return;
        var repair = RepairCatalog.Get(id);
        if (repair is null) return;

        StatusText = $"Ejecutando: {repair.Titulo}...";
        _ = Task.Run(async () =>
        {
            var result = await repair.ExecuteAsync();
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                StatusText = result.Success
                    ? $"✅ {repair.Titulo} completado."
                    : $"❌ {repair.Titulo} falló: {result.Error}");
        });
    }

    private async Task ExportPdfAsync()
    {
        if (!TieneDatos) return;
        StatusText = "Generando PDF...";
        try
        {
            var path = Path.Combine(
                Environment.CurrentDirectory, "informes",
                $"informe_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            await Services.InformeService.GenerateAsync(_diagnostico, path);
            StatusText = $"✅ PDF guardado: {path}";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Error al generar PDF: {ex.Message}";
        }
    }

    private static List<ModuloCheck> BuildModulos(Diagnostico diag)
    {
        var modulos = new List<ModuloCheck>
        {
            new("CPU", diag.Hardware?.CPU is not null, "CPU"),
            new("RAM", diag.Salud?.RamUsoPorcentaje < 90, "RAM"),
            new("Disco", diag.Hardware?.DiscosLogicos.All(d => d.UsoPorcentaje < 95) ?? false, "Disco"),
            new("Windows", diag.Windows?.EventosCriticos?.Count < 5, "Windows"),
        };
        return modulos;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public record ModuloCheck(string Nombre, bool Ok, string Icono);
