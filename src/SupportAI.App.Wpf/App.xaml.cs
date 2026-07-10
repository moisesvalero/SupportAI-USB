using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace SupportAI.App.Wpf;

public partial class App : Application
{
    private static readonly string CrashLogPath = Path.Combine(
        AppContext.BaseDirectory, "crash.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandled;
        DispatcherUnhandledException += OnDispatcherUnhandled;
        base.OnStartup(e);
    }

    private void OnDomainUnhandled(object sender, UnhandledExceptionEventArgs e)
    {
        WriteCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);
    }

    private void OnDispatcherUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrash("Dispatcher.UnhandledException", e.Exception);
        e.Handled = true;
        Shutdown(1);
    }

    private static void WriteCrash(string source, Exception? ex)
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            Directory.CreateDirectory(dir);
            var line = $"[{DateTime.Now:O}] {source}: {ex}\n";
            File.AppendAllText(CrashLogPath, line);
        }
        catch
        {
        }
    }
}
