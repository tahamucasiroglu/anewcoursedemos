namespace Konsept.Web.Models;

/// <summary>Üst bardaki profil seçimi (spec 0003 Req 8).</summary>
/// <param name="ProfileId">Seçilen profil.</param>
public sealed record SelectProfileForm(Guid? ProfileId);
