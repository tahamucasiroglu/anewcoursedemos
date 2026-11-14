using System.Diagnostics;
using TahaKonsept.Agent.Shell.Base;

namespace TahaKonsept.Agent.Shell.Concrete;

/// <summary>Windows kabuğu (<c>UseShellExecute</c>) ile tarayıcı/Gezgin açar.</summary>
public sealed class WindowsDesktopLauncher : DesktopLauncherBase
{
    /// <inheritdoc />
    protected override void Launch(string target, string? arguments)
    {
        var info = new ProcessStartInfo(target) { UseShellExecute = true };
        if (arguments is not null)
        {
            info.Arguments = arguments;
        }

        using var process = Process.Start(info);
    }
}
