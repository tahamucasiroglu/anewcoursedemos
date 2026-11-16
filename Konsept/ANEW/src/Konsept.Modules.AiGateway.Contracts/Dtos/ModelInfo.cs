namespace Konsept.Modules.AiGateway.Contracts.Dtos;

/// <summary>Bir katalog girişi ve isteği yapan kullanıcı için kullanılabilirliği (spec 0006 Req 2–4, 18).</summary>
/// <param name="Id">Model kimliği.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="Kind">Tür: <c>text</c> · <c>image</c> · <c>audio</c> · <c>video_engine</c>.</param>
/// <param name="Local">Yerel giriş (ajanda çalışır; key gerekmez).</param>
/// <param name="LanguageOnly">Tek desteklenen dil (<c>tr</c> · <c>en</c>) ya da <c>null</c>.</param>
/// <param name="LicenseWarning">"Lisans uyarısı" etiketi.</param>
/// <param name="Component">Listelenmesi için ajanda kurulu olması gereken bileşen ya da <c>null</c>.</param>
/// <param name="EffortLevels">Desteklenen efor seviyeleri.</param>
/// <param name="DefaultEffort">Varsayılan efor.</param>
/// <param name="Availability">Kullanılabilirlik: <see cref="Available"/> · <see cref="NoKey"/> · <see cref="NoPermission"/> · <see cref="Unreadable"/>.</param>
public sealed record ModelInfo(
    string Id,
    string Name,
    string Kind,
    bool Local,
    string? LanguageOnly,
    bool LicenseWarning,
    string? Component,
    IReadOnlyList<string> EffortLevels,
    string? DefaultEffort,
    string Availability)
{
    /// <summary>Kullanılabilir.</summary>
    public const string Available = "available";

    /// <summary>Sağlayıcı key'i yok.</summary>
    public const string NoKey = "no_key";

    /// <summary>Kullanma yetkisi yok.</summary>
    public const string NoPermission = "no_permission";

    /// <summary>Key okunamıyor.</summary>
    public const string Unreadable = "unreadable";

    /// <summary>Tür: metin.</summary>
    public const string KindText = "text";

    /// <summary>Tür: görsel.</summary>
    public const string KindImage = "image";

    /// <summary>Tür: ses.</summary>
    public const string KindAudio = "audio";

    /// <summary>Tür: video motoru.</summary>
    public const string KindVideoEngine = "video_engine";
}
