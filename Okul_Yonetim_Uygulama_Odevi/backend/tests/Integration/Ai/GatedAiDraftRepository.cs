using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Gerçek taslak deposunun sarmalayıcısı (review L-2): taslak okunduktan sonra <see cref="DraftHoldGate"/>'ten geçilir
/// (okuma ile silme/onay arasında bekleme); diğer işlemler olduğu gibi aktarılır.
/// </summary>
public sealed class GatedAiDraftRepository : IAiDraftRepository
{
    private readonly IAiDraftRepository inner;
    private readonly DraftHoldGate gate;

    public GatedAiDraftRepository(IAiDraftRepository inner, DraftHoldGate gate)
    {
        this.inner = inner;
        this.gate = gate;
    }

    public async Task<AiDraft?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        AiDraft? draft = await this.inner.FindAsync(id, cancellationToken).ConfigureAwait(false);
        await this.gate.PassAsync().ConfigureAwait(false);
        return draft;
    }

    public Task<AiDraft?> FindWeeklySummaryAsync(Guid studentId, DateOnly weekStart, CancellationToken cancellationToken) =>
        this.inner.FindWeeklySummaryAsync(studentId, weekStart, cancellationToken);

    public Task<IReadOnlyList<(Guid StudentId, AiDraftStatus Status)>> ListWeeklySummaryStatusesAsync(IReadOnlyCollection<Guid> studentIds, DateOnly weekStart, CancellationToken cancellationToken) =>
        this.inner.ListWeeklySummaryStatusesAsync(studentIds, weekStart, cancellationToken);

    public void Add(AiDraft draft) => this.inner.Add(draft);

    public void Remove(AiDraft draft) => this.inner.Remove(draft);
}
