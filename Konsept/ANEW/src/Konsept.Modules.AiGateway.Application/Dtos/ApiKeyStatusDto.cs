namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>
/// Ayarlar'daki bir sağlayıcı satırı (spec 0004 Req 9, 12): durum (<c>none</c> · <c>saved</c> · <c>unreadable</c>), yalnızca maskeli
/// key, doğrulama etiketi (<c>verified</c> · <c>unverified</c>) ve güncellenme zamanı. Tam key asla taşınmaz.
/// </summary>
/// <param name="ProviderId">Sağlayıcı kimliği.</param>
/// <param name="ProviderName">Sağlayıcının görünen adı.</param>
/// <param name="State">Durum.</param>
/// <param name="Hint">Maskeli key; kayıt yoksa ya da okunamıyorsa <c>null</c>.</param>
/// <param name="Validation">Doğrulama etiketi; kayıt yoksa <c>null</c>.</param>
/// <param name="UpdatedAt">Son güncellenme (UTC); kayıt yoksa <c>null</c>.</param>
public sealed record ApiKeyStatusDto(string ProviderId, string ProviderName, string State, string? Hint, string? Validation, DateTimeOffset? UpdatedAt)
{
    /// <summary>Durum: key girilmemiş.</summary>
    public const string StateNone = "none";

    /// <summary>Durum: key kayıtlı ve okunabiliyor.</summary>
    public const string StateSaved = "saved";

    /// <summary>Durum: key kayıtlı ama şifre çözme anahtarları olmadan çözülemiyor.</summary>
    public const string StateUnreadable = "unreadable";
}
