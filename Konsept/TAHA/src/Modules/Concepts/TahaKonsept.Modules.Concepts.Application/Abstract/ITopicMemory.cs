using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.Enums;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konu hafızası (profil başına): önerilen/üretilen/silinen/beğenilmeyen konuları parmak iziyle saklar; prompta token-tasarruflu
/// kısa liste verir (<see cref="RenderForPrompt"/>: en yeni önce, tavanlı, negatifler "✗"), tekrar elemesi tüm hafızayla yapılır (<see cref="IsKnown"/>).
/// </summary>
public interface ITopicMemory
{
    /// <summary>Profil hafızasını yükler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Anlık görüntü.</returns>
    Task<TopicMemorySnapshot> LoadAsync(Guid userId, Guid? profileId, CancellationToken cancellationToken = default);

    /// <summary>Başlıkları verilen türle kaydeder (varsa tür güçlenir).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="titles">Başlıklar.</param>
    /// <param name="kind">Tür.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task RememberTitlesAsync(Guid userId, Guid? profileId, IEnumerable<string> titles, TopicMemoryKind kind, CancellationToken cancellationToken = default);

    /// <summary>Konseptin hafıza kaydını yazar/günceller (üretildi, silindi, geri alındı).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="title">Başlık.</param>
    /// <param name="kind">Tür.</param>
    /// <param name="forceKind">true: tür zayıflasa da yazılır (geri alma).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task MarkConceptAsync(Guid userId, Guid? profileId, Guid conceptId, string title, TopicMemoryKind kind, bool forceKind = false, CancellationToken cancellationToken = default);

    /// <summary>Konsepte bağlı hafıza kayıtlarını siler (kalıcı silme).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task ForgetConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken = default);

    /// <summary>Hafızayı sıfırlar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="all">true: hepsi; false: yalnız öneri ve beğenilmeyenler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen kayıt sayısı.</returns>
    Task<int> ResetAsync(Guid userId, bool all, CancellationToken cancellationToken = default);

    /// <summary>Prompta girecek kısa listeyi üretir (boşsa boş dize).</summary>
    /// <param name="snapshot">Hafıza.</param>
    /// <returns>Satır listesi.</returns>
    string RenderForPrompt(TopicMemorySnapshot snapshot);

    /// <summary>Başlık hafızadaki bir konunun tekrarı mı.</summary>
    /// <param name="snapshot">Hafıza.</param>
    /// <param name="fingerprint">Başlığın parmak izi.</param>
    /// <returns>Biliniyor mu.</returns>
    bool IsKnown(TopicMemorySnapshot snapshot, TopicFingerprint fingerprint);
}
