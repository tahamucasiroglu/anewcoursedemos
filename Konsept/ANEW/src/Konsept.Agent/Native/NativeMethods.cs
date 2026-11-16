using System.Runtime.InteropServices;

namespace Konsept.Agent.Native;

/// <summary>Windows bildirimi için Win32 çağrıları (shell32 / user32).</summary>
internal static class NativeMethods
{
    /// <summary>NIM_ADD.</summary>
    public const int NimAdd = 0;

    /// <summary>NIM_DELETE.</summary>
    public const int NimDelete = 2;

    /// <summary>NIF_ICON.</summary>
    public const int NifIcon = 0x2;

    /// <summary>NIF_TIP.</summary>
    public const int NifTip = 0x4;

    /// <summary>NIF_INFO.</summary>
    public const int NifInfo = 0x10;

    /// <summary>NIIF_WARNING.</summary>
    public const int NiifWarning = 0x2;

    /// <summary>IDI_WARNING.</summary>
    public static readonly IntPtr IdiWarning = new(32515);

    /// <summary>Bildirim alanı ikonunu ekler/kaldırır.</summary>
    /// <param name="message">İşlem.</param>
    /// <param name="data">Veri.</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShellNotifyIcon(int message, ref NotifyIconData data);

    /// <summary>Sistem ikonunu yükler.</summary>
    /// <param name="instance">Modül (sistem ikonları için sıfır).</param>
    /// <param name="name">İkon kimliği.</param>
    /// <returns>İkon tutamacı.</returns>
    [DllImport("user32.dll", EntryPoint = "LoadIconW", CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr LoadIcon(IntPtr instance, IntPtr name);
}
