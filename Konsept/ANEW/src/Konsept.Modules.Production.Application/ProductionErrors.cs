using Konsept.Modules.Production.Application.Resources;
using Konsept.Modules.Production.Domain.Rules;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Production.Application;

/// <summary>Production'ın beklenen hataları (kararlı kod + Türkçe mesaj; spec 0006).</summary>
public static class ProductionErrors
{
    /// <summary>Gets oturum yok.</summary>
    public static Error SignInRequired => new("auth.required", ProductionText.SignInRequired, ErrorType.Unauthorized);

    /// <summary>Gets profil yok ya da başkasının (BR-1).</summary>
    public static Error ProfileNotFound => new("production.profile_not_found", ProductionText.ProfileNotFound, ErrorType.NotFound);

    /// <summary>Gets iş yok ya da başkasının (BR-1).</summary>
    public static Error JobNotFound => new("production.job_not_found", ProductionText.JobNotFound, ErrorType.NotFound);

    /// <summary>Gets hiç konsept seçilmemiş.</summary>
    public static Error NothingSelected => new("production.nothing_selected", ProductionText.NothingSelected, ErrorType.Validation);

    /// <summary>Gets yalnızca <i>Onaylandı</i> konsept sıraya alınır (Req 10, AC-10).</summary>
    public static Error NotApproved => new("production.not_approved", ProductionText.NotApproved, ErrorType.Conflict);

    /// <summary>Gets model listesi okunamadı (AC-22).</summary>
    public static Error ModelsUnavailable => new("production.models_unavailable", ProductionText.ModelsUnavailable, ErrorType.Unavailable);

    /// <summary>Gets seçilen model listede yok ya da türü yanlış.</summary>
    public static Error ModelNotListed => new("production.model_not_listed", ProductionText.ModelNotListed, ErrorType.Validation);

    /// <summary>Gets yetkisiz model seçimi (BR-13, AC-4).</summary>
    public static Error NoPermission => new("production.no_permission", ProductionText.NoPermission, ErrorType.Validation);

    /// <summary>Gets üretimdeki iş taşınamaz/çıkarılamaz (Req 11, AC-13).</summary>
    public static Error JobLocked => new("production.job_locked", ProductionText.JobLocked, ErrorType.Conflict);

    /// <summary>Gets uçtaki iş o yönde taşınamaz.</summary>
    public static Error CannotMove => new("production.cannot_move", ProductionText.CannotMove, ErrorType.Conflict);

    /// <summary>Gets durdurulacak iş yok.</summary>
    public static Error NothingRunning => new("production.nothing_running", ProductionText.NothingRunning, ErrorType.Conflict);

    /// <summary>Gets devam yalnızca Durduruldu/Hata'dan (Req 15).</summary>
    public static Error CannotResume => new("production.cannot_resume", ProductionText.CannotResume, ErrorType.Conflict);

    /// <summary>Gets işin onay bekleyen indirmesi yok (spec 0027 AC-6).</summary>
    public static Error NoPendingInstall => new("production.no_pending_install", ProductionText.NoPendingInstall, ErrorType.Conflict);

    /// <summary>Gets video yok ya da başkasının (BR-1).</summary>
    public static Error VideoNotFound => new("production.video_not_found", ProductionText.VideoNotFound, ErrorType.NotFound);

    /// <summary>Gets ajan bağlı değil; video izlenemez (spec 0017 Req 21).</summary>
    public static Error StreamAgentOffline => new("production.agent_offline", ProductionText.StreamAgentOffline, ErrorType.Conflict);

    /// <summary>Gets video dosyası ajan klasöründe yok.</summary>
    public static Error StreamFileMissing => new("production.file_missing", ProductionText.StreamFileMissing, ErrorType.NotFound);

    /// <summary>Dosya eylemi geçersiz (yalnızca <c>folder</c> / <c>player</c>).</summary>
    public static Error FileActionInvalid => new("production.file_action_invalid", ProductionText.FileActionInvalid, ErrorType.Validation);

    /// <summary>Ajan 10 sn içinde cevap vermedi (spec 0007 Req 4).</summary>
    public static Error FileActionTimeout => new("production.file_action_timeout", ProductionText.FileActionTimeout, ErrorType.Timeout);

    /// <summary>Ajan dosyayı açamadı.</summary>
    public static Error FileActionFailed => new("production.file_action_failed", ProductionText.FileActionFailed, ErrorType.Unavailable);

    /// <summary>Gets kalıcı silme için ajan bağlı değil (spec 0019 Req 4); kayıt Silinenler'de kalır.</summary>
    public static Error PurgeAgentOffline => new("production.purge_agent_offline", ProductionText.PurgeAgentOffline, ErrorType.Conflict);

    /// <summary>Gets bazı dosyalar silinemedi (spec 0019 Req 4); kayıt Silinenler'de kalır, tekrar denenebilir.</summary>
    public static Error PurgePartial => new("production.purge_partial", ProductionText.PurgePartial, ErrorType.Conflict);

    /// <summary>Gets videonun sırada/üretimde/durdurulmuş/ajan bekleyen sürümü var (spec 0020 Req 8, AC-7).</summary>
    public static Error VersionActive => new("production.version_active", ProductionText.VersionActive, ErrorType.Conflict);

    /// <summary>Gets videonun hata veren sürümü sırada (önce Tekrar dene / Sürümü kaldır; plan 0020 Risk 2).</summary>
    public static Error VersionFailedPending => new("production.version_failed_pending", ProductionText.VersionFailedPending, ErrorType.Conflict);

    /// <summary>Gets hiçbir ayar değişmedi (spec 0020 Req 1).</summary>
    public static Error VersionNoChange => new("production.version_no_change", ProductionText.VersionNoChange, ErrorType.Conflict);

    /// <summary>Gets online çağrı onayı yok (spec 0020 Req 3, H-3): iş oluşmaz.</summary>
    public static Error VersionOnlineConfirm => new("production.online_confirmation_required", ProductionText.VersionOnlineConfirm, ErrorType.Conflict);

    /// <summary>Gets önizlemeden sonra çalışacak aşamalar değişti (review r1 M-1): iş açılmaz, kullanıcı yeniden bakar.</summary>
    public static Error VersionPreviewChanged => new("production.version_preview_changed", ProductionText.VersionPreviewChanged, ErrorType.Conflict);

    /// <summary>Gets görsel modeli açılıp kapatılamaz (plan 0020).</summary>
    public static Error VersionImageToggle => new("production.version_image_toggle", ProductionText.VersionImageToggle, ErrorType.Validation);

    /// <summary>Gets hata veren sürüm yok (Tekrar dene / Sürümü kaldır).</summary>
    public static Error VersionNotFailed => new("production.version_not_failed", ProductionText.VersionNotFailed, ErrorType.Conflict);

    /// <summary>Gets yarım dosya için ajan bağlı değil (spec 0020 Req 10); sürüm kaydı kalır.</summary>
    public static Error VersionRemoveAgentOffline => new("production.version_remove_agent_offline", ProductionText.VersionRemoveAgentOffline, ErrorType.Conflict);

    /// <summary>Gets yarım dosya silinemedi; sürüm kaydı kalır.</summary>
    public static Error VersionRemovePartial => new("production.version_remove_partial", ProductionText.VersionRemovePartial, ErrorType.Conflict);

    /// <summary>Gets konsept değişmedi ("Yine de baştan üret" seçilmedi; spec 0020 Req 5).</summary>
    public static Error RegenerateUnchanged => new("production.regenerate_unchanged", ProductionText.RegenerateUnchanged, ErrorType.Conflict);

    /// <summary>Gets Yeniden üret bağlamı geçersiz (spec 0020 Req 15).</summary>
    public static Error RegenerateContextInvalid => new("production.regenerate_context_invalid", ProductionText.RegenerateContextInvalid, ErrorType.Conflict);

    /// <summary>Gets üretimi süren sürüm varken Sil (spec 0020 Req 12).</summary>
    public static Error DeleteWhileProducing => new("production.delete_while_producing", ProductionText.DeleteWhileProducing, ErrorType.Conflict);

    /// <summary>"Sırayla üret" pasif (Req 14): mesaj nedenin metni, <c>reason</c> kodu.</summary>
    /// <param name="reason">Neden kodu (<see cref="StartBlock"/>).</param>
    /// <returns>Hata.</returns>
    public static Error StartBlocked(string reason) =>
        new("production.start_blocked", BlockText(reason), ErrorType.Conflict) { Metadata = new Dictionary<string, object> { ["reason"] = reason } };

    /// <summary>Neden kodunun Türkçe metni.</summary>
    /// <param name="reason">Kod.</param>
    /// <returns>Metin.</returns>
    public static string BlockText(string reason) => reason switch
    {
        StartBlock.Empty => ProductionText.BlockEmpty,
        StartBlock.RunningHere => ProductionText.BlockRunningHere,
        StartBlock.RunningElsewhere => ProductionText.BlockRunningElsewhere,
        StartBlock.ModelsFailed => ProductionText.BlockModelsFailed,
        StartBlock.NoCodeModel => ProductionText.BlockNoCodeModel,
        StartBlock.NoPermission => ProductionText.BlockNoPermission,
        StartBlock.NoKey => ProductionText.BlockNoKey,
        StartBlock.AudioLanguage => ProductionText.BlockAudioLanguage,
        StartBlock.ImageLanguage => ProductionText.BlockImageLanguage,
        _ => ProductionText.BlockUnavailable,
    };
}
