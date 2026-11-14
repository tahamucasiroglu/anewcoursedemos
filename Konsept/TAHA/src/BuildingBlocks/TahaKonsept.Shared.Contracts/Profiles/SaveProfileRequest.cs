using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Shared.Contracts.Profiles;

/// <summary>
/// Profil oluşturma (<c>POST api/profiles</c>) ve güncelleme (<c>PUT api/profiles/{id}</c>) isteği.
/// Doğrulama: kanal adı zorunlu (≤120), dil kodu (ör. <c>tr</c>), süre 15–1800 sn, kanal içeriği ≤4000 karakter.
/// </summary>
public sealed record SaveProfileRequest
{
    /// <summary>Kanal adı.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Video dili (ör. <c>tr</c>).</summary>
    public string Language { get; init; } = "tr";

    /// <summary>Video biçimi.</summary>
    public VideoFormat Format { get; init; } = VideoFormat.Vertical;

    /// <summary>Hedef süre (saniye).</summary>
    public int TargetDurationSec { get; init; } = 60;

    /// <summary>Kanal içeriği.</summary>
    public string? ChannelDescription { get; init; }

    /// <summary>Varsayılan anlatım tonu (opsiyonel).</summary>
    public string? DefaultTone { get; init; }

    /// <summary>Varsayılan hedef izleyici (opsiyonel).</summary>
    public string? DefaultAudience { get; init; }

    /// <summary>Varsayılan anlatım biçimi (opsiyonel).</summary>
    public string? DefaultNarrativeStyle { get; init; }

    /// <summary>Kaynak şablon anahtarı (opsiyonel).</summary>
    public string? TemplateKey { get; init; }
}
