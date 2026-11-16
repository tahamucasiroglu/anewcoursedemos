namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>Profilin Konseptler sayfası ayarları ve seçilebilir metin modelleri (spec 0005).</summary>
/// <param name="ProfileId">Profil.</param>
/// <param name="Roles">Üç rolün seçimi (konsept, tekrar denetimi, konu önerisi).</param>
/// <param name="Tone">Ton kodu (<c>null</c> = Belirtilmedi).</param>
/// <param name="Audience">İzleyici kodu.</param>
/// <param name="Narration">Anlatım biçimi kodu.</param>
/// <param name="WordCheck">Kelime denetimi.</param>
/// <param name="Models">Kullanıcının seçebileceği metin modelleri.</param>
public sealed record ConceptSettingsDto(
    Guid ProfileId,
    IReadOnlyList<RoleModelDto> Roles,
    string? Tone,
    string? Audience,
    string? Narration,
    bool WordCheck,
    IReadOnlyList<SelectableModelDto> Models);
