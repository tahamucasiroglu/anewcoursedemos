using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// AI taslakları (spec 0012): kurum filtreli; sahiplik ve bağlam denetimi çağıranda (Ai modülünün sorgu ve komutları). Spec 0014:
/// öğrenci-hafta başına haftalık özet (tür <c>WeeklySummary</c> + öğrenci + haftanın Pazartesi'si) ve bir sınıfın öğrencileri için
/// haftalık durumlar (tek sorgu, izlemesiz projeksiyon — T7).
/// </summary>
public interface IAiDraftRepository
{
    /// <summary>Taslak (izlenen — onaylamak ya da silmek için); yoksa null.</summary>
    Task<AiDraft?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Öğrencinin o haftaki haftalık özet taslağı (izlenen — yeniden oluşturmak, onaylamak ya da silmek için); yoksa null.</summary>
    Task<AiDraft?> FindWeeklySummaryAsync(Guid studentId, DateOnly weekStart, CancellationToken cancellationToken);

    /// <summary>Verilen öğrencilerin o haftaki haftalık özet durumları (taslağı olmayan öğrenci listede yok).</summary>
    Task<IReadOnlyList<(Guid StudentId, AiDraftStatus Status)>> ListWeeklySummaryStatusesAsync(IReadOnlyCollection<Guid> studentIds, DateOnly weekStart, CancellationToken cancellationToken);

    void Add(AiDraft draft);

    void Remove(AiDraft draft);
}
