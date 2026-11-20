using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Ports;

/// <summary>AI taslakları (spec 0012): kurum filtreli; sahiplik ve bağlam denetimi çağıranda (Ai modülünün sorgu ve komutları).</summary>
public interface IAiDraftRepository
{
    /// <summary>Taslak (izlenen — onaylamak ya da silmek için); yoksa null.</summary>
    Task<AiDraft?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(AiDraft draft);

    void Remove(AiDraft draft);
}
