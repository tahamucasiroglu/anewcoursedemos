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
