using System.Windows;
using SupportAI.App.Wpf.Services;

namespace SupportAI.App.Wpf;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        var settings = SettingsService.Load();
        OpenRouterBox.Text = settings.OpenRouterKey ?? "";
        GeminiBox.Text = settings.GeminiKey ?? "";
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        SettingsService.Save(new AppSettings
        {
            OpenRouterKey = OpenRouterBox.Text.Trim(),
            GeminiKey = GeminiBox.Text.Trim()
        });
        DialogResult = true;
        Close();
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
