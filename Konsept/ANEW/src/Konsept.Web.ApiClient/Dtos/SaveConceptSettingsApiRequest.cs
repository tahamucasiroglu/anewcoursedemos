namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Konsept ayarlarını kaydetme gövdesi (tarayıcıdan gelir, Api'ye aynen iletilir; kurallar Api'de). Verilmeyen bölüm değişmez.</summary>
/// <param name="Models">Rol seçimleri.</param>
/// <param name="FineTune">İnce ayar.</param>
/// <param name="WordCheck">Kelime denetimi.</param>
public sealed record SaveConceptSettingsApiRequest(IReadOnlyList<RoleModelApiRequest>? Models, FineTuneApiRequest? FineTune, bool? WordCheck);
