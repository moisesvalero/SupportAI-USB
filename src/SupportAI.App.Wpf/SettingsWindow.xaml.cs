using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SupportAI.App.Wpf.Services;

namespace SupportAI.App.Wpf;

public partial class SettingsWindow : Window
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public SettingsWindow()
    {
        InitializeComponent();
        var settings = SettingsService.Load();
        OpenRouterBox.Text = settings.OpenRouterKey ?? "";
        GeminiBox.Text = settings.GeminiKey ?? "";
    }

    private async void TestOpenRouter_Click(object sender, RoutedEventArgs e)
    {
        var key = OpenRouterBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            SetStatus(OpenRouterStatus, ResultDetail, false, "Introduce una key primero.");
            return;
        }

        SetStatus(OpenRouterStatus, ResultDetail, null, "Probando OpenRouter...");
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://openrouter.ai/api/v1/auth/key");
            req.Headers.Add("Authorization", $"Bearer {key}");
            var resp = await _http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                SetStatus(OpenRouterStatus, ResultDetail, true, "OpenRouter: key válida ✅");
            }
            else
            {
                var body = await resp.Content.ReadAsStringAsync();
                SetStatus(OpenRouterStatus, ResultDetail, false, $"OpenRouter: error {(int)resp.StatusCode} — {ExtraerError(body)}");
            }
        }
        catch (Exception ex)
        {
            SetStatus(OpenRouterStatus, ResultDetail, false, $"OpenRouter: {ex.Message}");
        }
    }

    private async void TestGemini_Click(object sender, RoutedEventArgs e)
    {
        var key = GeminiBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            SetStatus(GeminiStatus, ResultDetail, false, "Introduce una key primero.");
            return;
        }

        SetStatus(GeminiStatus, ResultDetail, null, "Probando Gemini...");
        try
        {
            var resp = await _http.GetAsync($"https://generativelanguage.googleapis.com/v1/models?key={key}");
            if (resp.IsSuccessStatusCode)
            {
                SetStatus(GeminiStatus, ResultDetail, true, "Gemini: key válida ✅");
            }
            else
            {
                var body = await resp.Content.ReadAsStringAsync();
                SetStatus(GeminiStatus, ResultDetail, false, $"Gemini: error {(int)resp.StatusCode} — {ExtraerError(body)}");
            }
        }
        catch (Exception ex)
        {
            SetStatus(GeminiStatus, ResultDetail, false, $"Gemini: {ex.Message}");
        }
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

    private static void SetStatus(TextBlock statusBlock, TextBlock detailBlock, bool? ok, string detail)
    {
        statusBlock.Text = ok switch
        {
            true => "✅",
            false => "❌",
            null => "⏳"
        };
        statusBlock.Foreground = ok switch
        {
            true => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
            false => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
            null => new SolidColorBrush(Color.FromRgb(52, 152, 219))
        };
        detailBlock.Text = detail;
    }

    private static string ExtraerError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                if (err.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? "desconocido";
            }
        }
        catch { }
        return json[..Math.Min(json.Length, 120)];
    }
}
