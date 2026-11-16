namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Profil listesi, seçili profil, sınır ve şablonlar (<c>GET api/profiles</c>; spec 0003 Req 3, 5, 8, 9).</summary>
/// <param name="Items">Profiller (tr-TR alfabetik).</param>
/// <param name="SelectedId">Seçili profil; profil yoksa <c>null</c>.</param>
/// <param name="MaxProfiles">Profil sınırı.</param>
/// <param name="Templates">Hazır şablonlar.</param>
public sealed record ProfileListResponse(IReadOnlyList<ProfileResponse> Items, Guid? SelectedId, int MaxProfiles, IReadOnlyList<ProfileTemplateResponse> Templates);
