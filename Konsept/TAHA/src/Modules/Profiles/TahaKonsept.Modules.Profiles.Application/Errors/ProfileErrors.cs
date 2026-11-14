using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Modules.Profiles.Application.Errors;

/// <summary>
/// Profiles modülünün hata tanımları (kod + Türkçe mesaj).
/// </summary>
public static class ProfileErrors
{
    /// <summary>Profil bulunamadı.</summary>
    public static readonly Error NotFound = Error.NotFound("profiles.not_found", "Profil bulunamadı.");
}
