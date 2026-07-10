using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
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
    private bool _reparando;
    private CancellationTokenSource? _repairCts;
    private string? _problemaExpandidoId;
    private int _scanProgress;
    private string _faseActual = "";
    private bool _escaneando;

    public MainViewModel()
    {
        ScanCommand = new AsyncRelayCommand(async _ => await ScanAsync());
        ExportPdfCommand = new AsyncRelayCommand(async _ => await ExportPdfAsync());
        AnalyzeWithIaCommand = new AsyncRelayCommand(async _ => await AnalyzeWithIaAsync());
        RepairCommand = new RelayCommand(ExecuteRepair);
        CancelRepairCommand = new RelayCommand(_ => CancelRepair());
        DownloadModelCommand = new RelayCommand(_ => DownloadModel());
        AbrirAccionCommand = new RelayCommand(AbrirAccion);
        ToggleExpandirCommand = new RelayCommand(ToggleExpandir);
        IniciarServicioCommand = new AsyncRelayCommand(async param => await IniciarServicio(param));
        StatusText = "Listo. Haz clic en [Escanear] para diagnosticar.";
        RefreshModelStatus();

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        timer.Tick += (s, e) =>
        {
            RefreshModelStatus();
            if (ModeloDescargado)
            {
                timer.Stop();
            }
        };
        timer.Start();
    }

    public ICommand ScanCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand AnalyzeWithIaCommand { get; }
    public ICommand RepairCommand { get; }
    public ICommand CancelRepairCommand { get; }
    public ICommand DownloadModelCommand { get; }
    public ICommand AbrirAccionCommand { get; }
    public ICommand ToggleExpandirCommand { get; }
    public ICommand IniciarServicioCommand { get; }

    public string? ProblemaExpandidoId
    {
        get => _problemaExpandidoId;
        set { _problemaExpandidoId = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProblemaExpandido)); }
    }

    private void ToggleExpandir(object? param)
    {
        if (param is string id)
            ProblemaExpandidoId = ProblemaExpandidoId == id ? null : id;
    }

    private async Task IniciarServicio(object? param)
    {
        if (param is not string nombreCorto) return;

        var confirm = MessageBox.Show(
            $"Iniciar el servicio '{nombreCorto}' requiere permisos de administrador.\n\nSe abrirá el diálogo de UAC de Windows.\n¿Continuar?",
            "Permisos elevados requeridos",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Start-Service '{nombreCorto}'\"",
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = false
        };
        try
        {
            using var proc = new Process { StartInfo = psi };
            proc.Start();
            await proc.WaitForExitAsync();

            if (proc.ExitCode == 0)
            {
                StatusText = $"✅ Servicio '{nombreCorto}' iniciado correctamente.";
                var svc = _diagnostico.Windows?.ServiciosFallando.FirstOrDefault(s => s.NombreCorto == nombreCorto);
                if (svc is not null)
                {
                    _diagnostico = _diagnostico with
                    {
                        Windows = _diagnostico.Windows! with
                        {
                            ServiciosFallando = _diagnostico.Windows.ServiciosFallando
                                .Where(s => s.NombreCorto != nombreCorto).ToList()
                        }
                    };
                    OnPropertyChanged(nameof(ServiciosFallando));
                }
            }
            else
            {
                StatusText = $"❌ '{nombreCorto}': no se pudo iniciar (código {proc.ExitCode}). Prueba a ejecutar SupportAI como administrador.";
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            StatusText = $"⏹️ Inicio de '{nombreCorto}' cancelado por el usuario.";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ '{nombreCorto}': {ex.Message}";
        }
    }

    private void AbrirAccion(object? param)
    {
        if (param is not string target) return;
        try
        {
            if (target.StartsWith("expand:"))
            {
                ToggleExpandir(target["expand:".Length..]);
                return;
            }
            if (target.StartsWith("ms-settings:"))
            {
                Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                return;
            }
            if (target == "shutdown")
            {
                var confirm = MessageBox.Show(
                    "¿Reiniciar el equipo ahora?\nGuarda tu trabajo antes de continuar.",
                    "Reiniciar equipo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "/r /t 30",
                        UseShellExecute = true
                    });
                return;
            }
            if (target == "taskmgr")
            {
                Process.Start(new ProcessStartInfo { FileName = "taskmgr.exe", UseShellExecute = true });
                return;
            }
            if (target == "windowsdefender:")
            {
                Process.Start(new ProcessStartInfo { FileName = "windowsdefender:", UseShellExecute = true });
                return;
            }
            Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Error al abrir: {ex.Message}";
        }
    }

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
        set { _puntuacion = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScoreColor)); OnPropertyChanged(nameof(ScoreLabel)); }
    }

    public string ScoreColor => Puntuacion switch
    {
        >= 80 => "#27ae60",
        >= 50 => "#f39c12",
        _ => "#e74c3c"
    };

    public string ScoreLabel => Puntuacion switch
    {
        >= 80 => "Excelente",
        >= 60 => "Buena",
        >= 40 => "Regular",
        _ => "Crítica"
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

    public bool PuedeAnalizarConIa => _iaConsent && TieneDatos && !_iaCargando && !_reparando && !_escaneando;

    public bool Reparando
    {
        get => _reparando;
        set
        {
            _reparando = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PuedeReparar));
            OnPropertyChanged(nameof(PuedeAnalizarConIa));
        }
    }

    public bool PuedeReparar => !_reparando;

    public List<ModuloCheck> Modulos { get; set; } = [];
    public List<Problema> Problemas { get; set; } = [];
    public List<IRepairAction> Repairs => RepairCatalog.All.ToList();
    public bool HayProblemas => Problemas.Count > 0;
    public bool TieneDatos => Puntuacion > 0;
    public List<ServicioInfo> ServiciosFallando => (_diagnostico.Windows?.ServiciosFallando ?? [])
        .Where(s => !string.IsNullOrWhiteSpace(s.PathName) && ServiceExecutableExists(s) && !EsUpdaterConocido(s))
        .ToList();

    private static readonly string[] UpdaterPrefixes = ["edgeupdate", "edgeupdatem", "googleupdate", "googleupdater", "braveupdate", "brave", "firefoxupdate", "mozillamaintenance", "adobeupdate", "adobearm"];

    private static bool EsUpdaterConocido(ServicioInfo s) =>
        !string.IsNullOrWhiteSpace(s.NombreCorto) &&
        UpdaterPrefixes.Any(p => s.NombreCorto.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    public bool ProblemaExpandido => _problemaExpandidoId is not null;

    public bool Escaneando
    {
        get => _escaneando;
        set { _escaneando = value; OnPropertyChanged(); OnPropertyChanged(nameof(Escaneando)); }
    }
    public int ScanProgress
    {
        get => _scanProgress;
        set { _scanProgress = value; OnPropertyChanged(); }
    }
    public string FaseActual
    {
        get => _faseActual;
        set { _faseActual = value; OnPropertyChanged(); }
    }

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

    private string _modelStatus = "";
    public string ModelStatus
    {
        get => _modelStatus;
        set { _modelStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(ModelListo)); }
    }
    public bool ModelListo => ModeloDescargado;
    private static bool ModeloDescargado => ModelDownloader.ModelExists;

    private void DownloadModel()
    {
        RefreshModelStatus();
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ModelDownloader.LlamaCppUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void RefreshModelStatus()
    {
        ModelStatus = ModelDownloader.GetStatus();
    }

    public async Task ScanAsync()
    {
        Escaneando = true;
        ScanProgress = 0;
        Puntuacion = 0;
        IaExplicacion = "";
        IaProvider = "";
        IaRecomendaciones = [];
        _problemaExpandidoId = null;
        OnPropertyChanged(nameof(ProblemaExpandidoId));

        // Timer anima progreso mientras PowerShell trabaja
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        var steps = new[] { ("Recopilando hardware...", 15), ("Analizando sistema...", 35), ("Revisando eventos...", 55), ("Verificando red y seguridad...", 70), ("Procesando resultados...", 85) };
        int stepIdx = 0;
        timer.Tick += (_, _) =>
        {
            if (stepIdx < steps.Length && ScanProgress >= steps[stepIdx].Item2)
            {
                FaseActual = steps[stepIdx].Item1;
                stepIdx++;
            }
            if (ScanProgress < 85) ScanProgress += 2;
        };
        timer.Start();

        try
        {
            StatusText = "Iniciando diagnóstico...";
            FaseActual = "Recopilando hardware...";

            _diagnostico = await _engine.CollectAllAsync(CancellationToken.None);
            ScanProgress = 90;
            FaseActual = "Analizando resultados...";

            var (problemas, puntuacion) = DiagnosticEngine.Analyze(_diagnostico);
            _diagnostico = _diagnostico with { Problemas = problemas, Puntuacion = puntuacion };

            Puntuacion = puntuacion;
            Problemas = problemas;
            Modulos = BuildModulos(_diagnostico);

            RefreshBindings();
            ScanProgress = 100;
            StatusText = $"Diagnóstico completado. Puntuación: {Puntuacion}/100 ({ScoreLabel}). {Problemas.Count} problema(s).";
        }
        finally
        {
            timer.Stop();
            Escaneando = false;
        }
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

    private async void ExecuteRepair(object? parameter)
    {
        if (parameter is not string id) return;
        if (Reparando) return;

        var repair = RepairCatalog.Get(id);
        if (repair is null) return;

        if (repair.RequiresElevation)
        {
            var prompt = $"Esta reparación requiere permisos de administrador:\n\n{repair.Titulo}\n{repair.Descripcion}\n\nSe mostrará el diálogo de UAC de Windows para continuar.\n\n¿Continuar?";
            var confirm = MessageBox.Show(prompt, "Permisos elevados requeridos",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        Reparando = true;
        StatusText = $"Ejecutando: {repair.Titulo}...";
        _repairCts = new CancellationTokenSource();

        try
        {
            var result = await Task.Run(async () => await repair.ExecuteAsync(ct: _repairCts.Token), _repairCts.Token);
            StatusText = result.Success
                ? $"✅ {repair.Titulo} completado."
                : $"❌ {repair.Titulo} falló: {result.Error}";
        }
        catch (OperationCanceledException)
        {
            StatusText = $"⏹️ {repair.Titulo} cancelado por el usuario.";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ {repair.Titulo} falló: {ex.Message}";
        }
        finally
        {
            _repairCts?.Dispose();
            _repairCts = null;
            Reparando = false;
        }
    }

    private void CancelRepair()
    {
        if (_repairCts is not null && !_repairCts.IsCancellationRequested)
        {
            StatusText = "Cancelando reparación...";
            _repairCts.Cancel();
        }
    }

    private async Task ExportPdfAsync()
    {
        if (!TieneDatos) return;

        var dialog = new SaveFileDialog
        {
            Title = "Guardar informe PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"informe_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        };

        if (dialog.ShowDialog() != true) return;

        StatusText = "Generando PDF...";
        try
        {
            await Services.InformeService.GenerateAsync(_diagnostico, dialog.FileName);
            StatusText = $"✅ PDF guardado: {dialog.FileName}";
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
        OnPropertyChanged(nameof(ServiciosFallando));
    }

    private static bool ServiceExecutableExists(ServicioInfo svc)
    {
        var path = svc.PathName;
        if (string.IsNullOrWhiteSpace(path)) return false;

        path = path.Trim();
        // Extraer ejecutable: "C:\path\to\exe.exe" args → C:\path\to\exe.exe
        if (path.StartsWith('"'))
        {
            var end = path.IndexOf('"', 1);
            if (end > 0) path = path[1..end];
        }
        else
        {
            var space = path.IndexOf(' ');
            if (space > 0) path = path[..space];
        }

        // Expandir variables de entorno
        path = Environment.ExpandEnvironmentVariables(path);

        // Rutas relativas a SystemRoot: \SystemRoot\System32\... → C:\Windows\System32\...
        if (path.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
        {
            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            path = winDir + path[@"\SystemRoot".Length..];
        }

        // Rutas absolutas sin letra: \??\C:\... → C:\...
        if (path.StartsWith(@"\??\", StringComparison.OrdinalIgnoreCase))
            path = path[4..];

        return File.Exists(path);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public record ModuloCheck(string Nombre, bool Ok, string Icono);
