using System.Diagnostics;
using Konsept.Agent.Application.Base;
using Serilog;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Adresi işletim sisteminin varsayılan tarayıcısında açar; kabuk açamazsa (B-153) Windows Gezgini üzerinden dener, o da olmazsa hata loglanır.
/// </summary>
/// <param name="logger">Teknik log; verilmezse yazılmaz.</param>
/// <param name="start">Süreç başlatıcı (testte sahte); verilmezse <see cref="Process.Start(ProcessStartInfo)"/>.</param>
public sealed class ShellBrowserLauncher(ILogger? logger = null, Action<ProcessStartInfo>? start = null) : BrowserLauncherBase
{
    private readonly Action<ProcessStartInfo> _start = start ?? (info =>
    {
        using var process = Process.Start(info);
    });

    /// <inheritdoc />
    protected override void Launch(Uri address) =>
        _start(new ProcessStartInfo(address.AbsoluteUri) { UseShellExecute = true });

    /// <inheritdoc />
    protected override void LaunchFallback(Uri address)
    {
        var info = new ProcessStartInfo("explorer.exe") { UseShellExecute = false };
        info.ArgumentList.Add(address.AbsoluteUri);
        _start(info);
    }

    /// <inheritdoc />
    protected override void OnLaunchFailed(Uri address, Exception exception, bool recovered)
    {
        if (recovered)
        {
            logger?.Warning("Tarayıcı kabukla açılamadı, Gezgin üzerinden açıldı: {Host} {ExceptionType}", address.Host, exception.GetType().Name);
        }
        else
        {
            logger?.Error("Tarayıcı açılamadı: {Host} {ExceptionType}", address.Host, exception.GetType().Name);
        }
    }
}
