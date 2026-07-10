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
        Loaded += async (_, _) => await _vm.ScanAsync();
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
