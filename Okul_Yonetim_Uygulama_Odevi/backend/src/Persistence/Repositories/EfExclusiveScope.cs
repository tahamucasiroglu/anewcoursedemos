using Microsoft.EntityFrameworkCore.Storage;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Sıraya sokulmuş işlem: commit edilmeden kapatılırsa geri alınır; PostgreSQL kilidi işlemle birlikte bırakılır.</summary>
public sealed class EfExclusiveScope : IExclusiveScope
{
    private readonly IDbContextTransaction transaction;

    public EfExclusiveScope(IDbContextTransaction transaction)
    {
        this.transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken) => this.transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => this.transaction.DisposeAsync();
}
