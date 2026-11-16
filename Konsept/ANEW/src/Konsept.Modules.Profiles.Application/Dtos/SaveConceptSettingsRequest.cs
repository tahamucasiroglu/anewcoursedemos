namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>Konsept ayarlarını kaydetme isteği; verilmeyen (<c>null</c>) bölüm değişmez.</summary>
/// <param name="Models">Rol seçimleri (verilen roller güncellenir).</param>
/// <param name="FineTune">İnce ayar (üç grup birlikte).</param>
/// <param name="WordCheck">Kelime denetimi.</param>
public sealed record SaveConceptSettingsRequest(IReadOnlyList<RoleModelRequest>? Models, FineTuneRequest? FineTune, bool? WordCheck);
