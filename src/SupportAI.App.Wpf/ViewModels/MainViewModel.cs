using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SupportAI.Collectors.Windows;
using SupportAI.Core.Models;
using SupportAI.Ia;
using SupportAI.Repairs;

namespace SupportAI.App.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly PowerShellEngine _engine = new();
    private LlmService? _llm;
    private Diagnostico _diagnostico = new();
    private bool _iaConsent;

    public MainViewModel()
    {
        ScanCommand = new AsyncRelayCommand(async _ => await ScanAsync());
        ExportPdfCommand = new AsyncRelayCommand(async _ => await ExportPdfAsync());
        AnalyzeWithIaCommand = new AsyncRelayCommand(async _ => await AnalyzeWithIaAsync());
        RepairCommand = new RelayCommand(ExecuteRepair);
        StatusText = "Listo. Haz clic en [Escanear] para diagnosticar.";
    }

    public ICommand ScanCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand AnalyzeWithIaCommand { get; }
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
        set { _puntuacion = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScoreColor)); }
    }

    public string ScoreColor => Puntuacion switch
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
    public string RedInfo => _diagnostico.Red?.Internet == true ? "Conectado" : "Sin internet";

    public bool IaConsent
    {
        get => _iaConsent;
        set { _iaConsent = value; OnPropertyChanged(); OnPropertyChanged(nameof(PuedeAnalizarConIa)); }
    }

    public bool PuedeAnalizarConIa => _iaConsent && TieneDatos;

    public List<ModuloCheck> Modulos { get; set; } = [];
    public List<Problema> Problemas { get; set; } = [];
    public List<IRepairAction> Repairs => RepairCatalog.All.ToList();
    public bool HayProblemas => Problemas.Count > 0;
    public bool TieneDatos => Puntuacion > 0;

    // IA state
    private string _iaExplicacion = "";
    public string IaExplicacion
    {
        get => _iaExplicacion;
        set { _iaExplicacion = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneIaResultado)); }
    }

    private string _iaProvider = "";
    public string IaProvider
    {
        get => _iaProvider;
        set { _iaProvider = value; OnPropertyChanged(); }
    }

    private List<LlmRecomendacion> _iaRecomendaciones = [];
    public List<LlmRecomendacion> IaRecomendaciones
    {
        get => _iaRecomendaciones;
        set { _iaRecomendaciones = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneIaResultado)); }
    }

    public bool TieneIaResultado => !string.IsNullOrWhiteSpace(IaExplicacion);
    private bool _iaCargando;
    public bool IaCargando
    {
        get => _iaCargando;
        set { _iaCargando = value; OnPropertyChanged(); OnPropertyChanged(nameof(PuedeAnalizarConIa)); }
    }

    public async Task ScanAsync()
    {
        StatusText = "Escaneando...";
        Puntuacion = 0;
        IaExplicacion = "";
        IaProvider = "";
        IaRecomendaciones = [];

        _diagnostico = await _engine.CollectAllAsync(CancellationToken.None);
        var (problemas, puntuacion) = DiagnosticEngine.Analyze(_diagnostico);
        _diagnostico = _diagnostico with { Problemas = problemas, Puntuacion = puntuacion };

        Puntuacion = puntuacion;
        Problemas = problemas;
        Modulos = BuildModulos(_diagnostico);

        RefreshBindings();
        StatusText = $"Diagnóstico completado. Puntuación: {Puntuacion}/100. {Problemas.Count} problema(s).";
    }

    private async Task AnalyzeWithIaAsync()
    {
        if (!_iaConsent || !TieneDatos) return;
        IaCargando = true;
        IaExplicacion = "";
        IaProvider = "";
        IaRecomendaciones = [];
        StatusText = "Analizando con IA...";

        try
        {
            _llm ??= new LlmService(
                openRouterKey: GetKeyFromFile("OPENROUTER_KEY"),
                geminiKey: GetKeyFromFile("GEMINI_KEY"));

            var result = await _llm.AnalyzeAsync(_diagnostico);
            IaExplicacion = result.Explicacion;
            IaProvider = result.ProveedorUsado;
            IaRecomendaciones = result.Recomendaciones;
            StatusText = $"Análisis IA completado ({result.ProveedorUsado}).";
        }
        catch (Exception ex)
        {
            StatusText = $"Error en análisis IA: {ex.Message}";
            IaExplicacion = "No se pudo completar el análisis con IA. Verifica tu conexión y clave API.";
            IaProvider = "Error";
        }
        finally
        {
            IaCargando = false;
        }
    }

    private static string? GetKeyFromFile(string name)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $".{name}");
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
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
        return
        [
            new("CPU", diag.Hardware?.CPU is not null, "CPU"),
            new("RAM", diag.Salud?.RamUsoPorcentaje < 90, "RAM"),
            new("Disco", diag.Hardware?.DiscosLogicos.All(d => d.UsoPorcentaje < 95) ?? false, "Disco"),
            new("Red", diag.Red?.Internet == true, "Red"),
            new("Windows", diag.Windows?.EventosCriticos?.Count < 5, "Windows"),
            new("Seguridad", diag.Seguridad?.DefenderActivo == true, "Seguridad"),
            new("Drivers", diag.Drivers?.DispositivosError is null or { Count: 0 }, "Drivers"),
        ];
    }

    private void RefreshBindings()
    {
        OnPropertyChanged(nameof(Sistema));
        OnPropertyChanged(nameof(CpuInfo));
        OnPropertyChanged(nameof(RamInfo));
        OnPropertyChanged(nameof(RamUso));
        OnPropertyChanged(nameof(Uptime));
        OnPropertyChanged(nameof(RedInfo));
        OnPropertyChanged(nameof(Modulos));
        OnPropertyChanged(nameof(Problemas));
        OnPropertyChanged(nameof(HayProblemas));
        OnPropertyChanged(nameof(TieneDatos));
        OnPropertyChanged(nameof(PuedeAnalizarConIa));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public record ModuloCheck(string Nombre, bool Ok, string Icono);
