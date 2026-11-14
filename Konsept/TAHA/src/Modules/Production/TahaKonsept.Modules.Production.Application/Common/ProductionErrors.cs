using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Modules.Production.Application.Common;

/// <summary>
/// Production modülünün hata tanımları (kod + Türkçe mesaj). Kodlar <c>production.*</c> önekiyle başlar.
/// </summary>
public static class ProductionErrors
{
    /// <summary>Sıra öğesi bulunamadı.</summary>
    public static readonly Error QueueItemNotFound = Error.NotFound("production.queue_item_not_found", "Sıra öğesi bulunamadı.");

    /// <summary>İş bulunamadı.</summary>
    public static readonly Error JobNotFound = Error.NotFound("production.job_not_found", "Üretim işi bulunamadı.");

    /// <summary>Video bulunamadı.</summary>
    public static readonly Error VideoNotFound = Error.NotFound("production.video_not_found", "Video bulunamadı.");

    /// <summary>Okunuş kaydı bulunamadı.</summary>
    public static readonly Error PronunciationNotFound = Error.NotFound("production.pronunciation_not_found", "Okunuş kaydı bulunamadı.");

    /// <summary>Konsept bulunamadı.</summary>
    public static readonly Error ConceptNotFound = Error.NotFound("production.concept_not_found", "Konsept bulunamadı ya da silinmiş.");

    /// <summary>Konseptte sahne yok.</summary>
    public static readonly Error ConceptHasNoScenes = Error.Validation("production.concept_empty", "Konseptte sahne yok; önce konsepti tamamlayın.");

    /// <summary>Hiçbir konsept sıraya eklenemedi.</summary>
    public static readonly Error NothingToQueue = Error.Validation("production.nothing_to_queue", "Sıraya eklenecek konsept bulunamadı.");

    /// <summary>Sıra öğesi taşınamaz.</summary>
    public static readonly Error CannotMove = Error.Conflict("production.cannot_move", "Bu öğe bu yöne taşınamaz.");

    /// <summary>Bilinmeyen video motoru.</summary>
    public static Error UnknownEngine(string engine) =>
        Error.Validation("production.unknown_engine", "Bilinmeyen video motoru: " + engine);

    /// <summary>Aşama için işleyici yok.</summary>
    public static Error NoHandler(string stage) =>
        Error.Failure("production.no_handler", "Aşama işleyicisi bulunamadı: " + stage);

    /// <summary>Aşama bu yerde çalıştırılamaz.</summary>
    public static Error WrongLocation(string stage) =>
        Error.Failure("production.wrong_location", "Aşama bu yerde çalıştırılamaz: " + stage);

    /// <summary>Üst aşama çıktısı eksik.</summary>
    public static Error MissingOutput(string stage) =>
        Error.Failure("production.missing_output", "Önceki aşamanın çıktısı bulunamadı: " + stage);

    /// <summary>Sahne kodu geçersiz.</summary>
    public static Error InvalidSceneCode(string reason) =>
        Error.Validation("production.invalid_scene_code", "Sahne kodu geçersiz: " + reason);

    /// <summary>Online ses WAV dönmedi ya da birleştirilemedi.</summary>
    public static Error AudioCombineFailed(string reason) =>
        Error.Failure("production.audio_combine_failed", "Online sesler birleştirilemedi: " + reason);

    /// <summary>Model seçilmemiş.</summary>
    public static Error ModelMissing(string what) =>
        Error.Validation("production.model_missing", what + " modeli seçilmemiş.");
}
