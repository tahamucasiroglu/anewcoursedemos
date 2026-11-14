using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Shared.Contracts.Profiles;

/// <summary>
/// Kanal profilinin dışa açık görünümü (API cevabı ve modüller arası okuma: <c>IProfileReader</c>).
/// Önemli alanlar: <see cref="Name"/> (kanal adı), <see cref="Language"/> (video dili), <see cref="Format"/>,
/// <see cref="TargetDurationSec"/>, <see cref="ChannelDescription"/> (kanal içeriği; sistem promptuna girer), <see cref="IsActive"/>.
/// </summary>
public sealed record ProfileDto
{
    /// <summary>Profil kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Kanal adı.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Video dili (BCP-47 kısa kod, ör. <c>tr</c>, <c>en</c>, <c>de</c>).</summary>
    public string Language { get; init; } = "tr";

    /// <summary>Video biçimi (dikey/yatay).</summary>
    public VideoFormat Format { get; init; } = VideoFormat.Vertical;

    /// <summary>Hedef video süresi (saniye).</summary>
    public int TargetDurationSec { get; init; } = 60;

    /// <summary>Kanal içeriği: odak, tarih aralığı, kaçınılacaklar… (konsept üretiminde sistem promptuna eklenir).</summary>
    public string ChannelDescription { get; init; } = string.Empty;

    /// <summary>Varsayılan anlatım tonu (opsiyonel).</summary>
    public string? DefaultTone { get; init; }

    /// <summary>Varsayılan hedef izleyici (opsiyonel).</summary>
    public string? DefaultAudience { get; init; }

    /// <summary>Varsayılan anlatım biçimi (opsiyonel).</summary>
    public string? DefaultNarrativeStyle { get; init; }

    /// <summary>Profilin oluşturulduğu hazır şablonun anahtarı (opsiyonel).</summary>
    public string? TemplateKey { get; init; }

    /// <summary>Kullanıcının aktif profili mi (kullanıcı başına tek aktif profil).</summary>
    public bool IsActive { get; init; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Son güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
