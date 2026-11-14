using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Profiles.Domain.ValueObjects;

/// <summary>
/// Profilin düzenlenebilir alanları (oluşturma ve güncellemede tek parça taşınır). <see cref="Normalize"/>
/// metinleri kırpar, dil kodunu küçük harfe çevirir, boş opsiyonel alanları <c>null</c> yapar ve süreyi sınırlar içine alır.
/// </summary>
/// <param name="Name">Kanal adı.</param>
/// <param name="Language">Video dili.</param>
/// <param name="Format">Video biçimi.</param>
/// <param name="TargetDurationSec">Hedef süre (saniye).</param>
/// <param name="ChannelDescription">Kanal içeriği.</param>
/// <param name="DefaultTone">Varsayılan ton.</param>
/// <param name="DefaultAudience">Varsayılan izleyici.</param>
/// <param name="DefaultNarrativeStyle">Varsayılan anlatım biçimi.</param>
/// <param name="TemplateKey">Kaynak şablon anahtarı.</param>
public sealed record ProfileDetails(
    string Name,
    string Language,
    VideoFormat Format,
    int TargetDurationSec,
    string ChannelDescription,
    string? DefaultTone,
    string? DefaultAudience,
    string? DefaultNarrativeStyle,
    string? TemplateKey)
{
    /// <summary>Alanları normalleştirilmiş yeni bir kopya döndürür.</summary>
    /// <returns>Normalleştirilmiş ayrıntılar.</returns>
    public ProfileDetails Normalize() => this with
    {
        Name = (Name ?? string.Empty).Trim(),
        Language = string.IsNullOrWhiteSpace(Language) ? Constants.ProfileLimits.DefaultLanguage : Language.Trim().ToLowerInvariant(),
        TargetDurationSec = Math.Clamp(TargetDurationSec, Constants.ProfileLimits.MinDurationSec, Constants.ProfileLimits.MaxDurationSec),
        ChannelDescription = (ChannelDescription ?? string.Empty).Trim(),
        DefaultTone = NullIfBlank(DefaultTone),
        DefaultAudience = NullIfBlank(DefaultAudience),
        DefaultNarrativeStyle = NullIfBlank(DefaultNarrativeStyle),
        TemplateKey = NullIfBlank(TemplateKey),
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
