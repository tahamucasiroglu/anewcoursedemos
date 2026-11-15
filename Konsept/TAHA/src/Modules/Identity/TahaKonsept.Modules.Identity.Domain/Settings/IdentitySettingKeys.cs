namespace TahaKonsept.Modules.Identity.Domain.Settings;

/// <summary>
/// <see cref="Entities.IdentitySetting"/> anahtarları: <see cref="RegistrationOpen"/> (yeni kayıtlara izin, <c>true</c>/<c>false</c>;
/// satır yoksa açık sayılır).
/// </summary>
public static class IdentitySettingKeys
{
    /// <summary>Yeni kayıtlara izin ver.</summary>
    public const string RegistrationOpen = "registration.open";
}
