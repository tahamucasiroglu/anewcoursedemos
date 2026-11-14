using Avalonia;

namespace Konsept.Agent;

/// <summary>
/// Ajanın giriş noktası. Uygulama yalnızca tray menüsündeki "Çıkış" ile kapanır.
/// </summary>
internal static class Program
{
    /// <summary>Uygulamayı başlatır.</summary>
    /// <param name="args">Komut satırı argümanları.</param>
    /// <returns>Çıkış kodu.</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Serilog.Log.Fatal(e.ExceptionObject as Exception, "Ajanda beklenmeyen hata");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Serilog.Log.Error(e.Exception, "Ajanda gözlenmeyen görev hatası");
            e.SetObserved();
        };
        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
        }
        finally
        {
            Serilog.Log.CloseAndFlush();
        }
    }

    /// <summary>Avalonia uygulamasını yapılandırır (testler de kullanır).</summary>
    /// <returns>Uygulama kurucusu.</returns>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();
}
