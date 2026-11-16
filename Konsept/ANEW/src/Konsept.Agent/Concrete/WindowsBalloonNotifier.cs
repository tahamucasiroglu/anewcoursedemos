using System.Runtime.InteropServices;
using Konsept.Agent.Base;
using Konsept.Agent.Native;

namespace Konsept.Agent.Concrete;

/// <summary>
/// Windows bildirimi: geçici bir bildirim alanı ikonu üzerinden balon (Windows 10/11'de bildirim olarak görünür, plan 0022 Risk 3);
/// birkaç saniye sonra ikon kaldırılır. Windows dışında ya da pencere tutamacı yoksa gösterilmez.
/// </summary>
/// <param name="windowHandle">Bildirimin bağlanacağı pencere tutamacını veren kaynak.</param>
public sealed class WindowsBalloonNotifier(Func<IntPtr> windowHandle) : DesktopNotifierBase
{
    private const int IconId = 0x2022;
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(12);

    /// <inheritdoc />
    protected override void Display(string title, string message)
    {
        var handle = windowHandle();
        if (!OperatingSystem.IsWindows() || handle == IntPtr.Zero)
        {
            return;
        }

        var data = new NotifyIconData
        {
            Size = Marshal.SizeOf<NotifyIconData>(),
            Window = handle,
            Id = IconId,
            Flags = NativeMethods.NifIcon | NativeMethods.NifTip | NativeMethods.NifInfo,
            Icon = NativeMethods.LoadIcon(IntPtr.Zero, NativeMethods.IdiWarning),
            Tip = title,
            Info = message,
            InfoTitle = title,
            InfoFlags = NativeMethods.NiifWarning,
        };
        NativeMethods.ShellNotifyIcon(NativeMethods.NimDelete, ref data);
        if (!NativeMethods.ShellNotifyIcon(NativeMethods.NimAdd, ref data))
        {
            return;
        }

        _ = Task.Delay(Lifetime).ContinueWith(
            _ =>
            {
                var remove = new NotifyIconData { Size = Marshal.SizeOf<NotifyIconData>(), Window = handle, Id = IconId };
                NativeMethods.ShellNotifyIcon(NativeMethods.NimDelete, ref remove);
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
    }
}
