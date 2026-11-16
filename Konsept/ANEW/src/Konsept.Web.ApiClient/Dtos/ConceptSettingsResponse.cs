namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Profilin Konseptler sayfası ayarları ve seçilebilir metin modelleri (spec 0005; Api'den aynen).</summary>
/// <param name="ProfileId">Profil.</param>
/// <param name="Roles">Üç rolün seçimi.</param>
/// <param name="Tone">Ton kodu (<c>null</c> = Belirtilmedi).</param>
/// <param name="Audience">İzleyici kodu.</param>
/// <param name="Narration">Anlatım biçimi kodu.</param>
/// <param name="WordCheck">Kelime denetimi.</param>
/// <param name="Models">Seçilebilir metin modelleri.</param>
public sealed record ConceptSettingsResponse(
    Guid ProfileId,
    IReadOnlyList<RoleModelResponse> Roles,
    string? Tone,
    string? Audience,
    string? Narration,
    bool WordCheck,
    IReadOnlyList<SelectableModelResponse> Models);
