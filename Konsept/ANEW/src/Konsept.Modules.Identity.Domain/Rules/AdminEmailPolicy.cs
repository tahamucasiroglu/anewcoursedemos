using Konsept.Modules.Identity.Domain.Enums;

namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>
/// İlk admin kuralı (spec 0011 Req 5): yalnızca ortam ayarındaki tek e-postaya ait hesap admin olur. Karşılaştırma 0002
/// Req 2'nin kuralıyla yapılır (<see cref="EmailNormalizer"/>: kırpma + kültürden bağımsız büyük harf, ordinal eşitlik);
/// ayar boşsa hiçbir hesap admin olmaz. Rol yalnızca bu kuraldan çıkar, istekteki hiçbir alandan değil.
/// </summary>
public static class AdminEmailPolicy
{
    /// <summary>Ayardaki e-postanın karşılaştırma biçimi.</summary>
    /// <param name="configuredAdminEmail">Ayardaki e-posta.</param>
    /// <returns>Normalleştirilmiş e-posta; ayar boş ya da yalnızca boşluksa <c>null</c>.</returns>
    public static string? NormalizedAdminEmail(string? configuredAdminEmail) =>
        string.IsNullOrWhiteSpace(configuredAdminEmail) ? null : EmailNormalizer.Normalize(configuredAdminEmail);

    /// <summary>E-posta ayardaki admin e-postası mı.</summary>
    /// <param name="configuredAdminEmail">Ayardaki e-posta.</param>
    /// <param name="email">Hesabın e-postası.</param>
    /// <returns>Eşleşiyorsa <c>true</c>; ayar boşsa her zaman <c>false</c>.</returns>
    public static bool IsAdminEmail(string? configuredAdminEmail, string? email) =>
        RoleForNormalized(NormalizedAdminEmail(configuredAdminEmail), EmailNormalizer.Normalize(email)) == Role.Admin;

    /// <summary>Normalleştirilmiş e-postanın rolü (açılış senkronu ile aynı karar).</summary>
    /// <param name="normalizedAdminEmail">Ayardaki e-postanın normalleştirilmiş biçimi; ayar boşsa <c>null</c>.</param>
    /// <param name="normalizedEmail">Hesabın normalleştirilmiş e-postası.</param>
    /// <returns>Eşleşirse <see cref="Role.Admin"/>, değilse <see cref="Role.User"/>.</returns>
    public static Role RoleForNormalized(string? normalizedAdminEmail, string normalizedEmail) =>
        normalizedAdminEmail is not null && string.Equals(normalizedAdminEmail, normalizedEmail, StringComparison.Ordinal)
            ? Role.Admin
            : Role.User;
}
