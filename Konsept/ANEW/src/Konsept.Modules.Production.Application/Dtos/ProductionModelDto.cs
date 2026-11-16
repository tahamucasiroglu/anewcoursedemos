namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Model seçimi modalındaki seçenek (spec 0006 Req 2–4): yerel/online, tek dil, lisans uyarısı ve kullanılabilirlik etiketleri.</summary>
/// <param name="Id">Model kimliği.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="Kind">Tür (<c>text</c> · <c>image</c> · <c>audio</c> · <c>video_engine</c>).</param>
/// <param name="Local">Yerel.</param>
/// <param name="LanguageOnly">Tek dil (<c>tr</c> · <c>en</c>) ya da <c>null</c>.</param>
/// <param name="LicenseWarning">"Lisans uyarısı".</param>
/// <param name="EffortLevels">Efor seviyeleri.</param>
/// <param name="DefaultEffort">Varsayılan efor.</param>
/// <param name="Availability">Kullanılabilirlik.</param>
public sealed record ProductionModelDto(
    string Id,
    string Name,
    string Kind,
    bool Local,
    string? LanguageOnly,
    bool LicenseWarning,
    IReadOnlyList<string> EffortLevels,
    string? DefaultEffort,
    string Availability);
