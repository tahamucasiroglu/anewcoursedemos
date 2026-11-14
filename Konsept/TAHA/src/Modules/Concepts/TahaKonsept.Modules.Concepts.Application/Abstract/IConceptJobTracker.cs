using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// İş izleyicisi (singleton, bellekte): işlerin anlık durumunu tutar, kullanıcının güncel işini verir, iptali yönetir.
/// Kullanıcı başına son birkaç biten iş saklanır.
/// </summary>
public interface IConceptJobTracker
{
    /// <summary>Yeni işi "sırada" olarak kaydeder.</summary>
    /// <param name="job">İş.</param>
    /// <returns>İlk durum.</returns>
    ConceptJobDto Register(ConceptJob job);

    /// <summary>Kullanıcının güncel işi (çalışan → sıradaki → en son biten; yoksa Idle).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Durum.</returns>
    ConceptJobDto GetCurrent(Guid userId);

    /// <summary>İş durumunu günceller (kilit altında).</summary>
    /// <param name="jobId">İş.</param>
    /// <param name="update">Güncelleme.</param>
    /// <returns>Yeni durum (iş yoksa null).</returns>
    ConceptJobDto? Update(Guid jobId, Func<ConceptJobDto, ConceptJobDto> update);

    /// <summary>İşin iptal belirteci (iş yoksa iptal edilmiş belirteç).</summary>
    /// <param name="jobId">İş.</param>
    /// <returns>Belirteç.</returns>
    CancellationToken GetCancellationToken(Guid jobId);

    /// <summary>İş iptal edilmiş mi.</summary>
    /// <param name="jobId">İş.</param>
    /// <returns>İptal mi.</returns>
    bool IsCancelled(Guid jobId);

    /// <summary>Kullanıcının çalışan ve sıradaki işlerini iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Güncel durum.</returns>
    ConceptJobDto CancelAll(Guid userId);
}
