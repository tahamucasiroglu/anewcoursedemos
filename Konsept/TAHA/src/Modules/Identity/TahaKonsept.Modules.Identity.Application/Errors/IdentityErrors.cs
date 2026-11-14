using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Modules.Identity.Application.Errors;

/// <summary>
/// Identity modülünün hata tanımları (kod + Türkçe mesaj). Kodlar <c>identity.*</c> önekiyle başlar.
/// </summary>
public static class IdentityErrors
{
    /// <summary>Kurulum zaten yapılmış (kayıt kapalı).</summary>
    public static readonly Error SetupCompleted =
        Error.Conflict("identity.setup_completed", "Kurulum zaten tamamlanmış; yeni hesap açılamaz.");

    /// <summary>Kullanıcı adı ya da parola hatalı.</summary>
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("identity.invalid_credentials", "Kullanıcı adı ya da parola hatalı.");

    /// <summary>Çok fazla hatalı deneme nedeniyle geçici kilit.</summary>
    public static readonly Error LockedOut =
        Error.Unauthorized("identity.locked_out", "Çok fazla hatalı deneme yapıldı. Lütfen birkaç dakika sonra tekrar deneyin.");

    /// <summary>Kullanıcı yasaklı.</summary>
    public static readonly Error UserBanned =
        Error.Forbidden("identity.user_banned", "Hesabınız engellenmiş.");

    /// <summary>Refresh token geçersiz, süresi dolmuş ya da iptal edilmiş.</summary>
    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("identity.invalid_refresh_token", "Oturum süresi doldu. Lütfen tekrar giriş yapın.");

    /// <summary>Kullanılmış refresh token tekrar geldi; aile iptal edildi.</summary>
    public static readonly Error RefreshTokenReuse =
        Error.Unauthorized("identity.refresh_token_reuse", "Oturum güvenlik nedeniyle sonlandırıldı. Lütfen tekrar giriş yapın.");

    /// <summary>Oturum açmış kullanıcı yok.</summary>
    public static readonly Error NotAuthenticated =
        Error.Unauthorized("identity.not_authenticated", "Oturum açmanız gerekiyor.");

    /// <summary>Kullanıcı bulunamadı.</summary>
    public static readonly Error UserNotFound =
        Error.NotFound("identity.user_not_found", "Kullanıcı bulunamadı.");

    /// <summary>Son etkin yönetici yasaklanamaz.</summary>
    public static readonly Error LastAdmin =
        Error.Conflict("identity.last_admin", "Son etkin yönetici engellenemez.");

    /// <summary>Identity işlemi doğrulama hatalarıyla başarısız oldu.</summary>
    /// <param name="messages">Hata mesajları.</param>
    /// <returns>Hata.</returns>
    public static Error IdentityFailed(IEnumerable<string> messages)
    {
        var list = messages.ToArray();
        return Error.Validation(
            "identity.validation",
            list.Length > 0 ? string.Join(" ", list) : "İşlem doğrulanamadı.",
            new Dictionary<string, string[]> { ["identity"] = list });
    }
}
