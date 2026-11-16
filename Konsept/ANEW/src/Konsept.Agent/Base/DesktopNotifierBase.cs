using Konsept.Agent.Abstract;

namespace Konsept.Agent.Base;

/// <summary>Bildirimlerin ortak kuralı: metinler sınıra kırpılır, gösterim hatası ajanı etkilemez. Gösterim alt sınıftadır.</summary>
public abstract class DesktopNotifierBase : IDesktopNotifier
{
    /// <summary>Başlığın en fazla uzunluğu (Windows sınırı 63).</summary>
    public const int MaxTitleLength = 63;

    /// <summary>Metnin en fazla uzunluğu (Windows sınırı 255).</summary>
    public const int MaxMessageLength = 255;

    /// <inheritdoc />
    public void Show(string title, string message)
    {
        try
        {
            Display(Cut(title, MaxTitleLength), Cut(message, MaxMessageLength));
        }
#pragma warning disable CA1031 // Bildirim en iyi çabadır; pencere zaten öne gelir.
        catch (Exception)
#pragma warning restore CA1031
        {
        }
    }

    /// <summary>Bildirimi gösterir.</summary>
    /// <param name="title">Başlık.</param>
    /// <param name="message">Metin.</param>
    protected abstract void Display(string title, string message);

    private static string Cut(string? text, int max)
    {
        var value = text ?? string.Empty;
        return value.Length > max ? value[..(max - 1)] + "…" : value;
    }
}
