namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>
/// Modeller listesindeki satır (spec 0004 Req 4, 6): katalog değerleri ve isteği yapan kullanıcıya göre kullanılabilirlik etiketi
/// (<c>no_permission</c> · <c>available</c> · <c>no_key</c> · <c>unreadable</c>).
/// </summary>
/// <param name="Id">Model kimliği.</param>
/// <param name="ProviderId">Sağlayıcı kimliği.</param>
/// <param name="ProviderName">Sağlayıcı adı.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="Kind">Tür (<c>text</c> · <c>image</c> · <c>audio</c> · <c>video_engine</c>; spec 0006 Req 18).</param>
/// <param name="EffortLevels">Desteklenen efor seviyeleri.</param>
/// <param name="DefaultEffort">Varsayılan efor.</param>
/// <param name="OutputTokenLimit">Çıktı token limiti (metin).</param>
/// <param name="TimeoutSeconds">Çağrı zaman aşımı (sn).</param>
/// <param name="InputPricePerMillionUsd">1M giriş token'ı USD.</param>
/// <param name="OutputPricePerMillionUsd">1M çıkış token'ı USD.</param>
/// <param name="PricePerImageUsd">Görsel başına USD.</param>
/// <param name="Availability">Kullanılabilirlik.</param>
/// <param name="Local">Yerel giriş (spec 0006 Req 18; key ve fiyat yok).</param>
/// <param name="LanguageOnly">Tek desteklenen dil (<c>tr</c> · <c>en</c>) ya da <c>null</c>.</param>
/// <param name="LicenseWarning">"Lisans uyarısı" etiketi.</param>
/// <param name="Component">Bağlı ajan bileşeni ya da <c>null</c>.</param>
public sealed record CatalogModelDto(
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
    string Availability,
    bool Local = false,
    string? LanguageOnly = null,
    bool LicenseWarning = false,
    string? Component = null)
{
    /// <summary>Tür: metin.</summary>
    public const string KindText = "text";

    /// <summary>Tür: görsel.</summary>
    public const string KindImage = "image";

    /// <summary>Tür: ses (spec 0006).</summary>
    public const string KindAudio = "audio";

    /// <summary>Tür: video motoru (spec 0006).</summary>
    public const string KindVideoEngine = "video_engine";

    /// <summary>Etiket: sağlayıcının key'i kayıtlı.</summary>
    public const string Available = "available";

    /// <summary>Etiket: sağlayıcının key'i yok.</summary>
    public const string NoKey = "no_key";

    /// <summary>Etiket: kullanıcının bu modeli kullanma yetkisi yok (spec 0013 Req 3; key durumundan önce gelir).</summary>
    public const string NoPermission = "no_permission";

    /// <summary>Etiket: key kayıtlı ama çözülemiyor.</summary>
    public const string Unreadable = "unreadable";
}
