using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>
/// Gerçek sınıf deposunun sarmalayıcısı (review L-1): sıra içindeki ad kontrolü (<see cref="ActiveNameExistsAsync"/>)
/// <see cref="RaceGate"/>'ten geçer; diğer işlemler olduğu gibi aktarılır.
/// </summary>
public sealed class GatedClassroomRepository : IClassroomRepository
{
    private readonly IClassroomRepository inner;
    private readonly RaceGate gate;

    public GatedClassroomRepository(IClassroomRepository inner, RaceGate gate)
    {
        this.inner = inner;
        this.gate = gate;
    }

    public Task<IReadOnlyList<Classroom>> ListByTermAsync(Guid termId, CancellationToken cancellationToken) => this.inner.ListByTermAsync(termId, cancellationToken);

    public Task<Classroom?> FindAsync(Guid id, CancellationToken cancellationToken) => this.inner.FindAsync(id, cancellationToken);

    public Task<IReadOnlyList<Classroom>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) => this.inner.ListByIdsAsync(ids, cancellationToken);

    public async Task<bool> ActiveNameExistsAsync(Guid termId, Guid branchId, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        await this.gate.PassAsync().ConfigureAwait(false);
        return await this.inner.ActiveNameExistsAsync(termId, branchId, name, exceptId, cancellationToken).ConfigureAwait(false);
    }

    public void Add(Classroom classroom) => this.inner.Add(classroom);
}
