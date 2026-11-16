namespace Konsept.Agent.Abstract;

/// <summary>İşletim sistemi bildirimi (spec 0022 Req 4: ajanda hata olunca Windows bildirimi gösterilir).</summary>
public interface IDesktopNotifier
{
    /// <summary>Bildirimi gösterir; gösterilemezse sessizce geçer.</summary>
    /// <param name="title">Başlık.</param>
    /// <param name="message">Metin.</param>
    void Show(string title, string message);
}
