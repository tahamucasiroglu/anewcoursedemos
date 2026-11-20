using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>
/// Gerçek dönem deposunun sarmalayıcısı (review L-1): sıra içinde çakışma kontrolü için yapılan okuma
/// (<see cref="ListAsync"/>) <see cref="RaceGate"/>'ten geçer; diğer işlemler olduğu gibi aktarılır.
/// </summary>
public sealed class GatedTermRepository : ITermRepository
{
    private readonly ITermRepository inner;
    private readonly RaceGate gate;

    public GatedTermRepository(ITermRepository inner, RaceGate gate)
    {
        this.inner = inner;
        this.gate = gate;
    }

    public async Task<IReadOnlyList<Term>> ListAsync(CancellationToken cancellationToken)
    {
        await this.gate.PassAsync().ConfigureAwait(false);
        return await this.inner.ListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<Term?> FindAsync(Guid id, CancellationToken cancellationToken) => this.inner.FindAsync(id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken) => this.inner.NameExistsAsync(name, exceptId, cancellationToken);

    public void Add(Term term) => this.inner.Add(term);
}
