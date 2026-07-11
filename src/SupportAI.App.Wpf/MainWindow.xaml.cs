using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using SupportAI.App.Wpf.ViewModels;

namespace SupportAI.App.Wpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        _vm.PropertyChanged += OnVmPropertyChanged;
        Loaded += async (_, _) => await _vm.ScanAsync();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ChatHabilitado) && _vm.ChatHabilitado)
        {
            Dispatcher.BeginInvoke(() =>
            {
                ChatInputBox.Focus();
                Keyboard.Focus(ChatInputBox);
                ChatScrollViewer?.ScrollToBottom();
            });
        }
        else if (e.PropertyName == nameof(MainViewModel.ChatMessages))
        {
            Dispatcher.BeginInvoke(() =>
            {
                ChatScrollViewer?.ScrollToBottom();
            });
        }
    }

    private void VerComandos_Click(object sender, RoutedEventArgs e)
    {
        var help = """
SupportAI USB — Comandos CLI:
  --diagnose           Ejecutar diagnóstico completo
  --json               Salida en JSON
  --out <archivo>      Guardar salida en archivo
  --repair <id>        Ejecutar reparación
  --dry-run            Simular reparación
  --list-repairs       Listar reparaciones
  --help               Esta ayuda
""";
        MessageBox.Show(help, "Comandos CLI", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (_vm.ChatSendCommand.CanExecute(null))
                _vm.ChatSendCommand.Execute(null);
        }
    }
}
