namespace Konsept.Web.ApiClient.Dtos;

/// <summary>
/// Ayarlar'daki sağlayıcı satırı (<c>api/ai/keys</c>; spec 0004 Req 9): durum (<c>none</c> · <c>saved</c> · <c>unreadable</c>),
/// yalnızca maskeli key, doğrulama etiketi ve güncellenme zamanı. Tam key asla gelmez.
/// </summary>
/// <param name="ProviderId">Sağlayıcı kimliği.</param>
/// <param name="ProviderName">Sağlayıcı adı.</param>
/// <param name="State">Durum.</param>
/// <param name="Hint">Maskeli key.</param>
/// <param name="Validation">Doğrulama etiketi (<c>verified</c> · <c>unverified</c>).</param>
/// <param name="UpdatedAt">Güncellenme zamanı (UTC).</param>
public sealed record ApiKeyStatusResponse(string ProviderId, string ProviderName, string State, string? Hint, string? Validation, DateTimeOffset? UpdatedAt);
