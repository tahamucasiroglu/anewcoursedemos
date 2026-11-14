using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Shared.Contracts.Profiles;

/// <summary>
/// Hazır kanal profili şablonu (kullanıcıya ait değil, sabit JSON'dan okunur). ➕ penceresindeki şablon seçici
/// bu alanlarla formu doldurur. Önemli alanlar: <see cref="Key"/>, <see cref="Title"/> (ör. "Tarih – Bizans"),
/// <see cref="Name"/> (örnek kanal adı), <see cref="ChannelDescription"/> (örnek kanal içeriği).
/// </summary>
public sealed record ProfileTemplateDto
{
    /// <summary>Şablon anahtarı (ör. <c>tarih-bizans</c>).</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Kategori (ör. Tarih, Teknoloji, Bilim).</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Şablonun görünen adı (ör. "Tarih – Bizans").</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Örnek kanal adı.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Video dili.</summary>
    public string Language { get; init; } = "tr";

    /// <summary>Video biçimi.</summary>
    public VideoFormat Format { get; init; } = VideoFormat.Vertical;

    /// <summary>Hedef süre (saniye).</summary>
    public int TargetDurationSec { get; init; } = 60;

    /// <summary>Örnek kanal içeriği.</summary>
    public string ChannelDescription { get; init; } = string.Empty;

    /// <summary>Önerilen anlatım tonu.</summary>
    public string? DefaultTone { get; init; }

    /// <summary>Önerilen hedef izleyici.</summary>
    public string? DefaultAudience { get; init; }

    /// <summary>Önerilen anlatım biçimi.</summary>
    public string? DefaultNarrativeStyle { get; init; }
}
