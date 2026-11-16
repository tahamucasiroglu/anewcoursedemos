namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>Seçili profili değiştirme isteği (spec 0003 Req 8–9).</summary>
/// <param name="ProfileId">Seçilecek profil.</param>
public sealed record SelectProfileRequest(Guid? ProfileId);
