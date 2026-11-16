using System.Runtime.InteropServices;

namespace Konsept.Agent.Native;

/// <summary>Win32 <c>NOTIFYICONDATAW</c> yapısı (yalnızca Windows bildirimi için).</summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NotifyIconData
{
    /// <summary>Yapı boyutu.</summary>
    public int Size;

    /// <summary>Pencere tutamacı.</summary>
    public IntPtr Window;

    /// <summary>İkon kimliği.</summary>
    public int Id;

    /// <summary>Geçerli alanlar.</summary>
    public int Flags;

    /// <summary>Geri çağrı mesajı.</summary>
    public int CallbackMessage;

    /// <summary>İkon.</summary>
    public IntPtr Icon;

    /// <summary>İpucu metni.</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Tip;

    /// <summary>Durum.</summary>
    public int State;

    /// <summary>Durum maskesi.</summary>
    public int StateMask;

    /// <summary>Bildirim metni.</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Info;

    /// <summary>Zaman aşımı ya da sürüm.</summary>
    public int TimeoutOrVersion;

    /// <summary>Bildirim başlığı.</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string InfoTitle;

    /// <summary>Bildirim bayrakları.</summary>
    public int InfoFlags;

    /// <summary>İkon GUID'i.</summary>
    public Guid Item;

    /// <summary>Balon ikonu.</summary>
    public IntPtr BalloonIcon;
}
