using Avalonia;
using TahaKonsept.Agent.SingleInstance.Concrete;
using Velopack;

namespace TahaKonsept.Agent;

/// <summary>
/// Giriş noktası: önce Velopack kurulum/güncelleme kancaları (<c>VelopackApp.Build().Run()</c>), sonra tek örnek denetimi
/// (ikinci örnek çalışan örneği öne getirip kapanır), sonra Avalonia (açık kapatmaya kadar çalışır; pencereler kapansa da tepside sürer).
/// </summary>
public static class Program
{
    /// <summary>Tek örnek kilit adı.</summary>
    public const string SingleInstanceName = "TahaKonsept.Agent";

    /// <summary>Uygulamayı başlatır.</summary>
    /// <param name="args">Komut satırı (<c>--tray</c>: oturum açılışı, pencere açılmaz).</param>
    /// <returns>Çıkış kodu.</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        VelopackApp.Build().Run();

        using var guard = new NamedMutexSingleInstanceGuard(SingleInstanceName);
        if (!guard.TryAcquire())
        {
            guard.SignalExistingInstance();
            return 0;
        }

        return AppBuilder.Configure(() => new App(guard, args))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
    }
}
