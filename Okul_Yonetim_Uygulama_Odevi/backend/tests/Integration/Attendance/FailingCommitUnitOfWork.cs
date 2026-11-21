using OkulYonetim.Application.Ports;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Gerçek iş biriminin sarmalayıcısı (0010 VERIFY V-4, plan Risks 4): yoklama kaydının sıralı işlemi ("attendance:" anahtarı)
/// commit edilirken hata verir — kayıt ve bildirim satırları yazılmış ama işlem kalıcı olmamıştır; işlem kapanınca geri alınır.
/// Diğer işlemler olduğu gibi aktarılır.
/// </summary>
public sealed class FailingCommitUnitOfWork : IUnitOfWork
{
    private const string AttendanceKeyPrefix = "attendance:";
    private readonly IUnitOfWork inner;

    public FailingCommitUnitOfWork(IUnitOfWork inner)
    {
        this.inner = inner;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => this.inner.SaveChangesAsync(cancellationToken);

    public void DiscardChanges() => this.inner.DiscardChanges();

    public async Task<IExclusiveScope> BeginExclusiveAsync(string key, CancellationToken cancellationToken)
    {
        IExclusiveScope scope = await this.inner.BeginExclusiveAsync(key, cancellationToken).ConfigureAwait(false);
        return key.StartsWith(AttendanceKeyPrefix, StringComparison.Ordinal) ? new FailingScope(scope) : scope;
    }

    public async Task<IExclusiveScope> BeginExclusiveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keys);
        IExclusiveScope scope = await this.inner.BeginExclusiveAsync(keys, cancellationToken).ConfigureAwait(false);
        return keys.Any(k => k.StartsWith(AttendanceKeyPrefix, StringComparison.Ordinal)) ? new FailingScope(scope) : scope;
    }

    private sealed class FailingScope : IExclusiveScope
    {
        private readonly IExclusiveScope inner;

        public FailingScope(IExclusiveScope inner)
        {
            this.inner = inner;
        }

        public Task CommitAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("test: yoklama işlemi commit edilemedi");

        public ValueTask DisposeAsync() => this.inner.DisposeAsync();
    }
}
