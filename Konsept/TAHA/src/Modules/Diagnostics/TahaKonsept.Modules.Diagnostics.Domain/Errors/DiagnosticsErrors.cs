using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Domain.Errors;

/// <summary>
/// Diagnostics modülünün hata tanımları (kod önekleri <c>diagnostics.*</c>, Türkçe mesajlar).
/// </summary>
public static class DiagnosticsErrors
{
    /// <summary>Hata bildirimi bulunamadı.</summary>
    public static Error ErrorReportNotFound { get; } =
        Error.NotFound("diagnostics.error_report_not_found", "Hata bildirimi bulunamadı.");

    /// <summary>Yönetici yetkisi gerekli.</summary>
    public static Error AdminRequired { get; } =
        Error.Forbidden("diagnostics.admin_required", "Bu işlem için yönetici yetkisi gerekli.");

    /// <summary>Oturum gerekli.</summary>
    public static Error AuthenticationRequired { get; } =
        Error.Unauthorized("diagnostics.authentication_required", "Bu işlem için oturum açmanız gerekiyor.");

    /// <summary>Kullanıcı dizini (Identity) henüz bağlı değil.</summary>
    public static Error UserDirectoryUnavailable { get; } =
        Error.Failure("diagnostics.user_directory_unavailable", "Kullanıcı listesi şu an kullanılamıyor (kimlik modülü bağlı değil).");

    /// <summary>Kendini yasaklama denemesi.</summary>
    public static Error CannotBanSelf { get; } =
        Error.Validation("diagnostics.cannot_ban_self", "Kendi hesabınızı yasaklayamazsınız.");

    /// <summary>Kullanıcı bulunamadı.</summary>
    public static Error UserNotFound { get; } =
        Error.NotFound("diagnostics.user_not_found", "Kullanıcı bulunamadı.");

    /// <summary>İzinsiz durum geçişi.</summary>
    /// <param name="from">Mevcut durum.</param>
    /// <param name="to">Hedef durum.</param>
    /// <returns>Hata.</returns>
    public static Error InvalidStatusTransition(ErrorReportStatus from, ErrorReportStatus to) =>
        Error.Validation("diagnostics.invalid_status_transition", $"Bildirim durumu '{Label(from)}' iken '{Label(to)}' yapılamaz.");

    private static string Label(ErrorReportStatus status) => status switch
    {
        ErrorReportStatus.New => "Yeni",
        ErrorReportStatus.Seen => "Görüldü",
        ErrorReportStatus.Resolved => "Çözüldü",
        _ => status.ToString(),
    };
}
