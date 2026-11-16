using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using TahaKonsept.Agent.DependencyInjection;
using TahaKonsept.Agent.Headless;
using TahaKonsept.Agent.Headless.Abstract;
using TahaKonsept.Agent.SingleInstance.Concrete;
using TahaKonsept.Agent.Startup.Concrete;
using Velopack;

namespace TahaKonsept.Agent;

/// <summary>
/// Giriş noktası: önce Velopack kurulum/güncelleme kancaları (<c>VelopackApp.Build().Run()</c>), sonra tek örnek denetimi
/// (ikinci örnek çalışan örneği öne getirip kapanır), sonra Avalonia (açık kapatmaya kadar çalışır; pencereler kapansa da tepside sürer).
/// <c>--headless</c> verilirse Avalonia açılmaz: <see cref="IHeadlessAgentRunner"/> çalışır (geliştirici/e2e modu; bkz. <see cref="HeadlessLaunchOptions"/>).
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
        // Kaldırılırken Windows başlangıç (HKCU Run) kaydı da silinir; yoksa silinmiş exe'yi gösteren kayıt kalır.
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ => new RegistryStartupRegistration().SetEnabled(false))
            .Run();

        using var guard = new NamedMutexSingleInstanceGuard(SingleInstanceName);
        if (!guard.TryAcquire())
        {
            if (HeadlessLaunchOptions.IsHeadless(args))
            {
                return 3;
            }

            guard.SignalExistingInstance();
            return 0;
        }

        if (HeadlessLaunchOptions.IsHeadless(args))
        {
            return RunHeadless(HeadlessLaunchOptions.Parse(args));
        }

        return AppBuilder.Configure(() => new App(guard, args))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
    }

    /// <summary>Penceresiz modu çalıştırır: Ctrl+C, süreç sonu ya da <see cref="HeadlessLaunchOptions.StopFile"/> ile düzgün kapanır.</summary>
    /// <param name="options">Seçenekler.</param>
    /// <returns>Çıkış kodu.</returns>
    private static int RunHeadless(HeadlessLaunchOptions options)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        if (!string.IsNullOrWhiteSpace(options.StopFile))
        {
            if (File.Exists(options.StopFile))
            {
                File.Delete(options.StopFile);
            }

            _ = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (File.Exists(options.StopFile))
                    {
                        await cts.CancelAsync().ConfigureAwait(false);
                        return;
                    }

                    await Task.Delay(500).ConfigureAwait(false);
                }
            });
        }

        // Hub bağlantısı yalnız IAsyncDisposable: kapsayıcı DisposeAsync ile kapatılmalı (senkron Dispose kapanışta istisna fırlatıyordu).
        var services = new ServiceCollection()
            .AddHeadlessAgent(DesktopAgentServiceCollectionExtensions.CreateAgentOptions())
            .BuildServiceProvider();
        try
        {
            return services.GetRequiredService<IHeadlessAgentRunner>().RunAsync(options, cts.Token).GetAwaiter().GetResult();
        }
        finally
        {
            services.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
