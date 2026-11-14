using System.Diagnostics;
using TahaKonsept.Agent.Core.Shell.Base;

namespace TahaKonsept.Agent.Core.Shell.Concrete;

/// <summary>Windows kabuk başlatıcı: <c>explorer.exe /select,"dosya"</c> ve <c>UseShellExecute</c> ile varsayılan uygulama.</summary>
public sealed class WindowsShellLauncher : ShellLauncherBase
{
    /// <inheritdoc />
    protected override void LaunchFolder(string fullPath)
    {
        var info = new ProcessStartInfo("explorer.exe") { UseShellExecute = false };
        info.ArgumentList.Add("/select," + fullPath);
        using var process = Process.Start(info);
    }

    /// <inheritdoc />
    protected override void LaunchFile(string fullPath)
    {
        using var process = Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
    }
}
