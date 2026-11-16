namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Katalogdaki model ve kullanıcıya göre kullanılabilirlik etiketi (<c>available</c> · <c>no_key</c> · <c>unreadable</c>).</summary>
/// <param name="Id">Model kimliği.</param>
/// <param name="ProviderId">Sağlayıcı kimliği.</param>
/// <param name="ProviderName">Sağlayıcı adı.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="Kind">Tür (<c>text</c> · <c>image</c>).</param>
/// <param name="EffortLevels">Efor seviyeleri.</param>
/// <param name="DefaultEffort">Varsayılan efor.</param>
/// <param name="OutputTokenLimit">Çıktı token limiti.</param>
/// <param name="TimeoutSeconds">Zaman aşımı (sn).</param>
/// <param name="InputPricePerMillionUsd">1M giriş token'ı USD.</param>
/// <param name="OutputPricePerMillionUsd">1M çıkış token'ı USD.</param>
/// <param name="PricePerImageUsd">Görsel başına USD.</param>
/// <param name="Availability">Kullanılabilirlik.</param>
public sealed record CatalogModelResponse(
    string Id,
    string ProviderId,
    string ProviderName,
    string Name,
    string Kind,
    IReadOnlyList<string> EffortLevels,
    string? DefaultEffort,
    int? OutputTokenLimit,
    int TimeoutSeconds,
    decimal? InputPricePerMillionUsd,
    decimal? OutputPricePerMillionUsd,
    decimal? PricePerImageUsd,
    string Availability);
