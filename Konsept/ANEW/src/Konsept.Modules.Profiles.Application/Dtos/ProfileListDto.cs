namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>Üst bar profil seçimi ve modal için tek yanıt (spec 0003 Req 3, 5, 8, 9).</summary>
/// <param name="Items">Kullanıcının profilleri, kanal adına göre tr-TR alfabetik.</param>
/// <param name="SelectedId">Seçili profil (hatırlanan; yoksa ilk oluşturulan; profil yoksa <c>null</c>).</param>
/// <param name="MaxProfiles">Profil sınırı.</param>
/// <param name="Templates">Hazır şablonlar.</param>
public sealed record ProfileListDto(IReadOnlyList<ProfileDto> Items, Guid? SelectedId, int MaxProfiles, IReadOnlyList<ProfileTemplateDto> Templates);
