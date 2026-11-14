using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Modules.Agents.Domain.Errors;

/// <summary>
/// Agents modülünün hata kataloğu (kod + Türkçe mesaj). Cihaz eşleme yoklama hataları <c>DeviceTokenErrorCodes</c> ile aynı kodları kullanır.
/// </summary>
public static class AgentErrors
{
    /// <summary>Onay bekleniyor.</summary>
    public static readonly Error AuthorizationPending =
        Error.Validation("agents.device.authorization_pending", "Kod henüz web'de onaylanmadı.");

    /// <summary>Çok sık yoklama.</summary>
    public static readonly Error SlowDown =
        Error.Validation("agents.device.slow_down", "Çok sık soruluyor; yoklama aralığını artırın.");

    /// <summary>Kodun süresi doldu / kullanıldı.</summary>
    public static readonly Error ExpiredToken =
        Error.Validation("agents.device.expired_token", "Eşleme kodunun süresi doldu ya da kod zaten kullanıldı. Eşlemeyi yeniden başlatın.");

    /// <summary>Kullanıcı reddetti.</summary>
    public static readonly Error AccessDenied =
        Error.Validation("agents.device.access_denied", "Eşleme isteği reddedildi.");

    /// <summary>Cihaz kodu bilinmiyor (ajan eşlemeyi yeniden başlatmalı; kod <c>expired_token</c>).</summary>
    public static readonly Error InvalidDeviceCode =
        Error.Validation("agents.device.expired_token", "Cihaz kodu geçersiz. Eşlemeyi yeniden başlatın.");

    /// <summary>Kullanıcı kodu bulunamadı.</summary>
    public static readonly Error UserCodeNotFound =
        Error.NotFound("agents.device.user_code_not_found", "Bu kodla bekleyen bir eşleme isteği yok. Masaüstü programındaki kodu kontrol edin.");

    /// <summary>Kullanıcı kodunun süresi doldu.</summary>
    public static readonly Error UserCodeExpired =
        Error.Validation("agents.device.user_code_expired", "Kodun süresi doldu. Masaüstü programında eşlemeyi yeniden başlatın.");

    /// <summary>Eşleme isteği zaten sonuçlandı.</summary>
    public static readonly Error PairingAlreadyCompleted =
        Error.Conflict("agents.device.already_completed", "Bu eşleme isteği zaten sonuçlandı.");

    /// <summary>Cihaz bulunamadı.</summary>
    public static readonly Error DeviceNotFound =
        Error.NotFound("agents.device_not_found", "Masaüstü programı bulunamadı.");

    /// <summary>Çevrimiçi ajan yok.</summary>
    public static readonly Error AgentOffline =
        Error.Conflict("agents.offline", "Masaüstü programı çevrimiçi değil. Bilgisayarınızda programın açık olduğundan emin olun.");

    /// <summary>Ajan bağlantı bilgisi (LAN adresi/port/anahtar) henüz gelmedi.</summary>
    public static readonly Error AgentAddressUnknown =
        Error.Conflict("agents.address_unknown", "Masaüstü programının ağ adresi henüz bilinmiyor; program bağlandıktan birkaç saniye sonra tekrar deneyin.");

    /// <summary>Ajana komut gönderilemedi.</summary>
    public static readonly Error SendFailed =
        Error.Failure("agents.send_failed", "Masaüstü programına komut gönderilemedi.");

    /// <summary>Ses önizlemesi zaman aşımı.</summary>
    public static readonly Error PreviewTimeout =
        Error.Failure("agents.preview_timeout", "Masaüstü programı ses önizlemesine zamanında yanıt vermedi.");

    /// <summary>Artefakt bulunamadı.</summary>
    public static readonly Error ArtifactNotFound =
        Error.NotFound("agents.artifact_not_found", "Dosya bulunamadı ya da silinmiş.");

    /// <summary>Geçersiz artefakt adı.</summary>
    public static readonly Error ArtifactNameInvalid =
        Error.Validation("agents.artifact_name_invalid", "Dosya adı geçersiz.");

    /// <summary>Token üretilemedi.</summary>
    public static readonly Error TokenIssueFailed =
        Error.Failure("agents.token_issue_failed", "Masaüstü programı için oturum oluşturulamadı.");
}
