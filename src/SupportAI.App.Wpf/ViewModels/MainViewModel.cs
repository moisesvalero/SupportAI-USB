using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SupportAI.App.Wpf.Services;
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
    private bool _reparando;
    private CancellationTokenSource? _repairCts;
    private string? _problemaExpandidoId;
    private int _scanProgress;
    private string _faseActual = "";
    private bool _escaneando;
    private int _descargaProgreso;
    private bool _descargando;
    private string _descargaFase = "";
    private string _chatInput = "";
    private bool _chatCargando;
    private bool _chatInicializado;
    private string? _promptSistema;

    public MainViewModel()
    {
        ScanCommand = new AsyncRelayCommand(async _ => await ScanAsync());
        ExportPdfCommand = new AsyncRelayCommand(async _ => await ExportPdfAsync());
        AnalyzeWithIaCommand = new AsyncRelayCommand(async _ => await AnalyzeWithIaAsync());
        RepairCommand = new RelayCommand(ExecuteRepair);
        AbrirAccionCommand = new RelayCommand(AbrirAccion);
        IniciarServicioCommand = new AsyncRelayCommand(async param => await IniciarServicio(param));
        OpenSettingsCommand = new RelayCommand(_ => AbrirSettings());
        DescargarModeloCommand = new AsyncRelayCommand(async _ => await DescargarModeloAsync());
        ChatSendCommand = new AsyncRelayCommand(async _ => await ChatSendAsync());
        CheckUpdatesCommand = new AsyncRelayCommand(async _ => await CheckUpdatesAsync(true));
        OpenUpdateUrlCommand = new RelayCommand(_ => OpenUpdateUrl());
        
        StatusText = "Listo. Haz clic en [Escanear] para diagnosticar.";
        RefreshModelStatus();

        _ = CheckUpdatesAsync(false);

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
    public ICommand AbrirAccionCommand { get; }
    public ICommand IniciarServicioCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand DescargarModeloCommand { get; }
    public ICommand ChatSendCommand { get; }
    public ICommand CheckUpdatesCommand { get; }
    public ICommand OpenUpdateUrlCommand { get; }

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

        if (!System.Text.RegularExpressions.Regex.IsMatch(nombreCorto, @"^[a-zA-Z0-9._\- ]+$"))
        {
            StatusText = "❌ Nombre de servicio inválido.";
            return;
        }

        var confirm = MessageBox.Show(
            $"Iniciar el servicio '{nombreCorto}' requiere permisos de administrador.\n\nSe abrirá el diálogo de UAC de Windows.\n¿Continuar?",
            "Permisos elevados requeridos",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        var script = $"Start-Service '{nombreCorto}'";
        var bytes = System.Text.Encoding.Unicode.GetBytes(script);
        var encoded = Convert.ToBase64String(bytes);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}",
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
            StatusText = $"⏹️ Iniciar de '{nombreCorto}' cancelado por el usuario.";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ '{nombreCorto}': {ex.Message}";
        }
    }

    private void AbrirSettings()
    {
        var win = new SettingsWindow { Owner = Application.Current.MainWindow };
        win.ShowDialog();
        // Recreate LlmService on next use so it picks up new keys
        _llm = null;
    }

    private void AbrirAccion(object? param)
    {
        if (param is not string target) return;
        try
        {
            var whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "cleanmgr", "taskmgr", "shutdown", "windowsdefender:", "firewall.cpl",
                "devmgmt.msc", "eventvwr.msc", "diskmgmt.msc", "powercfg.cpl"
            };

            if (target.StartsWith("expand:"))
            {
                ToggleExpandir(target["expand:".Length..]);
                return;
            }
            if (target.StartsWith("ms-settings:"))
            {
                var allowedSettings = new[] { "ms-settings:network-troubleshoot", "ms-settings:network-status" };
                if (allowedSettings.Contains(target, StringComparer.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                }
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
            
            if (whitelist.Contains(target))
            {
                Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                return;
            }

            Trace.WriteLine($"[MainViewModel] Bloqueado intento de abrir acción no segura: {target}");
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
        set { _puntuacion = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScoreLabel)); }
    }

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
    public string CpuUso => _diagnostico.Salud is { } s ? $"{s.CpuUsoPorcentaje}%" : "";
    public string CpuTemp => _diagnostico.Salud is { CpuTemperatura: > 0 } s ? $"{s.CpuTemperatura}°C" : "N/D";
    public string CpuFreq => _diagnostico.Salud is { FrecuenciaActualMHz: > 0 } s ? $"{s.FrecuenciaActualMHz} MHz" : "";
    public string PlanEnergia => _diagnostico.Salud?.PlanEnergia ?? "";
    public string Latencia => _diagnostico.Red?.Internet == true && _diagnostico.Red.LatenciaMs > 0 ? $"{_diagnostico.Red.LatenciaMs} ms" : "N/D";
    public string BateriaInfo => _diagnostico.Hardware?.Bateria is { } b ? (b.Conectada ? $"🔌 {b.CargaPorcentaje}% (cargando)" : $"🔋 {b.CargaPorcentaje}%") : "";
    public string PageFileInfo => _diagnostico.Salud is { PageFileTotalMB: > 0 } s ? $"{s.PageFileUsadoMB}/{s.PageFileTotalMB} MB" : "N/D";

    public bool PuedeAnalizarConIa => TieneDatos && !_iaCargando && !_reparando && !_escaneando;

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

    public ObservableCollection<ModuloCheck> Modulos { get; } = [];
    public ObservableCollection<Problema> Problemas { get; } = [];
    public List<IRepairAction> Repairs => RepairCatalog.All.ToList();
    public bool HayProblemas => Problemas.Count > 0;
    public bool TieneDatos => _diagnostico.Hardware != null;
    public List<ServicioInfo> ServiciosFallando => (_diagnostico.Windows?.ServiciosFallando ?? [])
        .Where(s => s.EsCritico && !string.IsNullOrWhiteSpace(s.PathName) && ServiceExecutableExists(s))
        .ToList();
    public bool ProblemaExpandido => _problemaExpandidoId is not null;

    public bool Escaneando
    {
        get => _escaneando;
        set { _escaneando = value; OnPropertyChanged(); OnPropertyChanged(nameof(Escaneando)); OnPropertyChanged(nameof(PuedeAnalizarConIa)); }
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

    public bool Descargando
    {
        get => _descargando;
        set { _descargando = value; OnPropertyChanged(); OnPropertyChanged(nameof(PuedeDescargar)); }
    }
    public int DescargaProgreso
    {
        get => _descargaProgreso;
        set { _descargaProgreso = value; OnPropertyChanged(); }
    }
    public string DescargaFase
    {
        get => _descargaFase;
        set { _descargaFase = value; OnPropertyChanged(); }
    }
    public bool PuedeDescargar => !_descargando;

    private bool _updateAvailable;
    public bool UpdateAvailable
    {
        get => _updateAvailable;
        set { _updateAvailable = value; OnPropertyChanged(); }
    }
    
    private string _updateVersion = "";
    public string UpdateVersion
    {
        get => _updateVersion;
        set { _updateVersion = value; OnPropertyChanged(); }
    }
    
    private string _updateUrl = "";
    public string UpdateUrl
    {
        get => _updateUrl;
        set { _updateUrl = value; OnPropertyChanged(); }
    }

    public string IaProvider
    {
        get => _iaProvider;
        set { _iaProvider = value; OnPropertyChanged(); }
    }
    private string _iaProvider = "";

    private bool _iaCargando;
    public bool IaCargando
    {
        get => _iaCargando;
        set { _iaCargando = value; OnPropertyChanged(); OnPropertyChanged(nameof(PuedeAnalizarConIa)); }
    }

    public bool ChatHabilitado => _chatInicializado;

    private string _modelStatus = "";
    public string ModelStatus
    {
        get => _modelStatus;
        set { _modelStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(ModelListo)); }
    }
    public bool ModelListo => ModeloDescargado;
    private static bool ModeloDescargado => ModelDownloader.ModelExists;

    private void RefreshModelStatus()
    {
        ModelStatus = ModelDownloader.GetStatus();
    }

    private async Task CheckUpdatesAsync(bool manual)
    {
        if (manual) StatusText = "Buscando actualizaciones...";
        
        var (available, version, url) = await UpdateService.CheckForUpdatesAsync();
        
        if (available && url != null)
        {
            UpdateAvailable = true;
            UpdateVersion = version!;
            UpdateUrl = url;
            
            if (manual)
            {
                StatusText = $"Nueva versión disponible: {version}";
                var res = MessageBox.Show($"Hay una nueva versión disponible ({version}). ¿Deseas ir a la página de descargas?", "Actualización disponible", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (res == MessageBoxResult.Yes)
                {
                    OpenUpdateUrl();
                }
            }
        }
        else if (manual)
        {
            StatusText = "El programa está actualizado.";
            MessageBox.Show("Estás usando la última versión de SupportAI USB.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OpenUpdateUrl()
    {
        if (!string.IsNullOrWhiteSpace(UpdateUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = UpdateUrl, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Error al abrir enlace: {ex.Message}";
            }
        }
    }

    private async Task DescargarModeloAsync()
    {
        if (_descargando) return;
        Descargando = true;
        DescargaProgreso = 0;
        DescargaFase = "Iniciando descarga...";
        var progress = new Progress<int>(p => DescargaProgreso = p);

        try
        {
            await ModelDownloader.DownloadTinyModelAsync(progress, CancellationToken.None);
            DescargaFase = "Descarga completada";
            RefreshModelStatus();
            StatusText = "✅ Modelo descargado correctamente. Ahora puedes usar IA local.";
        }
        catch (OperationCanceledException)
        {
            DescargaFase = "Cancelado";
            StatusText = "⏹️ Descarga cancelada.";
        }
        catch (Exception ex)
        {
            DescargaFase = "Error";
            StatusText = $"❌ Error en descarga: {ex.Message}";
        }
        finally
        {
            Descargando = false;
        }
    }

    public async Task ScanAsync()
    {
        Escaneando = true;
        ScanProgress = 0;
        Puntuacion = 0;
        IaProvider = "";
        _chatInicializado = false;
        _promptSistema = null;
        ChatMessages.Clear();
        OnPropertyChanged(nameof(ChatMessages));
        OnPropertyChanged(nameof(ChatHabilitado));
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
            
            Problemas.Clear();
            foreach (var p in problemas)
                Problemas.Add(p);

            Modulos.Clear();
            foreach (var m in BuildModulos(_diagnostico))
                Modulos.Add(m);

            ScanProgress = 100;
            StatusText = $"Diagnóstico completado. Puntuación: {Puntuacion}/100 ({ScoreLabel}). {Problemas.Count} problema(s).";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Error al escanear: {ex.Message}";
            Trace.WriteLine($"[MainViewModel] Scan error: {ex.Message}");
        }
        finally
        {
            timer.Stop();
            Escaneando = false;
            RefreshBindings();
        }

        if (TieneDatos)
        {
            await AnalyzeWithIaAsync();
        }
    }

    private async Task AnalyzeWithIaAsync()
    {
        if (!TieneDatos || _chatInicializado) return;
        IaCargando = true;
        IaProvider = "";
        ChatMessages.Clear();
        _chatInicializado = false;
        _promptSistema = null;
        OnPropertyChanged(nameof(ChatMessages));
        OnPropertyChanged(nameof(ChatHabilitado));
        StatusText = "Analizando con IA...";

        try
        {
            _llm ??= new LlmService(
                openRouterKey: GetKeyFromFile("OPENROUTER_KEY"),
                geminiKey: GetKeyFromFile("GEMINI_KEY"));

            var result = await _llm.AnalyzeAsync(_diagnostico);
            IaProvider = result.ProveedorUsado;
            _chatInicializado = true;
            OnPropertyChanged(nameof(ChatHabilitado));

            var esReglas = result.ProveedorUsado == "Reglas locales";
            ChatMessages.Add(new ChatMessage("assistant", esReglas
                ? "⚠️ **Sin conexión IA.** No hay API key configurada ni modelo local.\n\nVe a ⚙️ (ajustes) para añadir una key de OpenRouter o Gemini, o descarga el modelo GGUF desde el panel lateral.\n\nMientras tanto, este es el análisis con reglas locales:\n\n" + result.Explicacion
                : result.Explicacion));
            OnPropertyChanged(nameof(ChatMessages));
            StatusText = $"Análisis completado ({result.ProveedorUsado}). Haz tu primera pregunta.";
        }
        catch (Exception ex)
        {
            ChatMessages.Add(new ChatMessage("assistant", $"❌ Error al analizar con IA: {ex.Message}\n\nVerifica tu conexión y las API keys en ⚙️."));
            OnPropertyChanged(nameof(ChatMessages));
            StatusText = "Error en análisis IA.";
        }
        finally
        {
            IaCargando = false;
        }
    }

    private static string? GetKeyFromFile(string name)
    {
        var settings = Services.SettingsService.Load();
        return name switch
        {
            "OPENROUTER_KEY" => settings.OpenRouterKey,
            "GEMINI_KEY" => settings.GeminiKey,
            _ => null
        };
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
        OnPropertyChanged(nameof(CpuUso));
        OnPropertyChanged(nameof(CpuTemp));
        OnPropertyChanged(nameof(CpuFreq));
        OnPropertyChanged(nameof(PlanEnergia));
        OnPropertyChanged(nameof(Latencia));
        OnPropertyChanged(nameof(BateriaInfo));
        OnPropertyChanged(nameof(PageFileInfo));
        OnPropertyChanged(nameof(Modulos));
        OnPropertyChanged(nameof(Problemas));
        OnPropertyChanged(nameof(HayProblemas));
        OnPropertyChanged(nameof(TieneDatos));
        OnPropertyChanged(nameof(PuedeAnalizarConIa));
        OnPropertyChanged(nameof(ServiciosFallando));
        OnPropertyChanged(nameof(ChatHabilitado));
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

    public ObservableCollection<ChatMessage> ChatMessages { get; } = [];
    public string ChatInput
    {
        get => _chatInput;
        set { _chatInput = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }
    public bool ChatCargando
    {
        get => _chatCargando;
        set { _chatCargando = value; OnPropertyChanged(); }
    }

    private async Task ChatSendAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatInput) || _chatCargando) return;
        var pregunta = ChatInput.Trim();
        ChatInput = "";
        OnPropertyChanged(nameof(ChatInput));

        ChatMessages.Add(new ChatMessage("user", pregunta));
        OnPropertyChanged(nameof(ChatMessages));
        ChatCargando = true;
        StatusText = "IA procesando pregunta...";

        try
        {
            _llm ??= new LlmService(
                openRouterKey: GetKeyFromFile("OPENROUTER_KEY"),
                geminiKey: GetKeyFromFile("GEMINI_KEY"));

            if (!_chatInicializado)
            {
                _chatInicializado = true;
                OnPropertyChanged(nameof(ChatHabilitado));
            }

            var messages = ConstruirMensajesParaLlm(pregunta);
            var response = await _llm.ChatAsync(messages);
            ChatMessages.Add(new ChatMessage("assistant", response));
            OnPropertyChanged(nameof(ChatMessages));
            StatusText = "Respuesta IA recibida.";
        }
        catch (Exception ex)
        {
            ChatMessages.Add(new ChatMessage("assistant", $"Error: {ex.Message}"));
            OnPropertyChanged(nameof(ChatMessages));
            StatusText = $"Error IA: {ex.Message}";
        }
        finally
        {
            ChatCargando = false;
            OnPropertyChanged(nameof(ChatCargando));
        }
    }

    private List<(string Role, string Text)> ConstruirMensajesParaLlm(string pregunta)
    {
        _promptSistema ??= BuildSystemPrompt();
        var msgs = new List<(string Role, string Text)>();
        msgs.Add(new("system", _promptSistema));
        foreach (var m in ChatMessages)
        {
            if (m.Role == "assistant" || m.Role == "user")
                msgs.Add(new(m.Role, m.Text));
        }
        msgs.Add(new("user", pregunta));
        return msgs;
    }

    private string BuildSystemPrompt()
    {
        var diagJson = System.Text.Json.JsonSerializer.Serialize(_diagnostico, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        return $"""
Eres un técnico especialista en diagnóstico de Windows y hardware 
(CPU, RAM, discos, drivers, BIOS, temperaturas, eventos del sistema, 
red, servicios). El usuario es un técnico de soporte. 
Responde en español, conciso y práctico. Pasos concretos, no teoría.

Datos del diagnóstico del equipo:
{diagJson}
""";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public record ModuloCheck(string Nombre, bool Ok, string Icono);
public record ChatMessage(string Role, string Text);
