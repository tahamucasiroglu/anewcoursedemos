using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Modules.Concepts.Domain.Errors;

/// <summary>
/// Concepts modülünün hata tanımları (kod + Türkçe mesaj). Durum geçiş hataları <see cref="ErrorType.Conflict"/> (409) döner.
/// </summary>
public static class ConceptErrors
{
    /// <summary>Konsept bulunamadı.</summary>
    public static readonly Error NotFound = Error.NotFound("concepts.not_found", "Konsept bulunamadı.");

    /// <summary>Düzeltme adayı bulunamadı (süresi dolmuş olabilir).</summary>
    public static readonly Error CandidateNotFound = Error.NotFound("concepts.candidate_not_found", "Düzeltilmiş sürüm bulunamadı; süresi dolmuş olabilir, tekrar üretin.");

    /// <summary>Silinmiş konsept üzerinde işlem yapılamaz.</summary>
    public static readonly Error IsDeleted = Error.Conflict("concepts.deleted", "Konsept silinmiş; önce Silinenler'den geri alın.");

    /// <summary>Konsept silinmiş değil (geri alma / kalıcı silme yalnız silinenlerde).</summary>
    public static readonly Error NotDeleted = Error.Conflict("concepts.not_deleted", "Bu işlem yalnızca Silinenler'deki konseptler için yapılabilir.");

    /// <summary>Belgede sahne yok.</summary>
    public static readonly Error EmptyDocument = Error.Validation("concepts.empty_document", "Konseptte en az bir sahne olmalı.");

    /// <summary>LLM boş / geçersiz konsept döndürdü.</summary>
    public static readonly Error InvalidModelOutput = Error.Failure("concepts.invalid_model_output", "Model geçerli bir konsept döndürmedi.");

    /// <summary>LLM hiç başlık önermedi.</summary>
    public static readonly Error NoTopics = Error.Failure("concepts.no_topics", "Model yeni başlık öneremedi; yönü değiştirip tekrar deneyin ya da Ayarlar'dan önerileri sıfırlayın.");

    /// <summary>Aktif profil yok.</summary>
    public static readonly Error NoProfile = Error.Validation("concepts.no_profile", "Önce üst bardan bir kanal profili oluşturun ya da seçin.");
}
