using System.Diagnostics;
using Konsept.Agent.Infrastructure.Base;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Windows kabuğuyla dosya eylemleri (spec 0007 Req 4): Klasörde aç → <c>explorer.exe /select,"yol"</c>; Sistem oynatıcısında aç → kabuk
/// (<c>UseShellExecute</c>) ile dosyanın varsayılan uygulaması. Yol tırnak içinde verilir (Windows yolunda tırnak olamaz).
/// </summary>
/// <param name="videoFolder">Video klasörünü veren kaynak.</param>
public sealed class ShellVideoFileActions(Func<string> videoFolder) : VideoFileActionsBase(videoFolder)
{
    /// <inheritdoc />
    protected override void RevealInFolder(string path)
    {
        // Windows yollarında çift tırnak geçersiz karakterdir; yol tırnak içinde tek parça kalır.
        using var process = Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = false });
    }

    /// <inheritdoc />
    protected override void OpenWithDefaultApp(string path)
    {
        using var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
