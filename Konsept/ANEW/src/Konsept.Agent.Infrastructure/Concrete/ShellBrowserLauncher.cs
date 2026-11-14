using System.Diagnostics;
using Konsept.Agent.Application.Base;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Adresi işletim sisteminin varsayılan tarayıcısında açar.
/// </summary>
public sealed class ShellBrowserLauncher : BrowserLauncherBase
{
    /// <inheritdoc />
    protected override void Launch(Uri address)
    {
        using var process = Process.Start(new ProcessStartInfo(address.AbsoluteUri) { UseShellExecute = true });
    }
}
