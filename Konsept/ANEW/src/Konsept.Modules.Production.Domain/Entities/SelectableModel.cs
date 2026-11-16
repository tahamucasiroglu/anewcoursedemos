namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>Kurallar için bir katalog girişinin özeti (spec 0006 Req 3–6): kimlik, tür, kullanılabilirlik ve tek dil etiketi.</summary>
/// <param name="Id">Model kimliği.</param>
/// <param name="Kind">Tür (<c>text</c> · <c>image</c> · <c>audio</c> · <c>video_engine</c>).</param>
/// <param name="Availability">Kullanılabilirlik (<c>available</c> · <c>no_key</c> · <c>unreadable</c> · <c>no_permission</c>).</param>
/// <param name="LanguageOnly">Tek desteklenen dil ya da <c>null</c>.</param>
public sealed record SelectableModel(string Id, string Kind, string Availability, string? LanguageOnly);
