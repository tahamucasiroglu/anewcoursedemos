using TahaKonsept.Modules.Production.Application.Abstract;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// <see cref="IJobCancellationScope"/> uygulaması: üst belirtece bağlı <see cref="CancellationTokenSource"/>; <see cref="RequestCancel"/>
/// iptal isteğini işaretler; dispose edilince sahibine (<c>onDispose</c>) haber verip kaynağı bırakır.
/// </summary>
public sealed class JobCancellationScope : IJobCancellationScope
{
    private readonly CancellationTokenSource _source;
    private readonly Action<JobCancellationScope> _onDispose;
    private int _requested;
    private int _disposed;

    /// <summary>Oluşturur.</summary>
    /// <param name="key">Kayıt anahtarı (kullanıcı, öğe).</param>
    /// <param name="parent">Üst belirteç.</param>
    /// <param name="onDispose">Dispose bildirimi (kayıttan silme).</param>
    public JobCancellationScope((Guid UserId, Guid QueueItemId) key, CancellationToken parent, Action<JobCancellationScope> onDispose)
    {
        Key = key;
        _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        _source = CancellationTokenSource.CreateLinkedTokenSource(parent);
        Token = _source.Token;
    }

    /// <summary>Kayıt anahtarı (kullanıcı, öğe).</summary>
    public (Guid UserId, Guid QueueItemId) Key { get; }

    /// <summary>Belirteç.</summary>
    public CancellationToken Token { get; }

    /// <summary>İptal isteği geldi mi.</summary>
    public bool CancelledByRequest => Volatile.Read(ref _requested) == 1;

    /// <summary>İptal ister (dispose edildiyse etkisiz).</summary>
    /// <returns>Bu çağrı iptal ettiyse <c>true</c>.</returns>
    public bool RequestCancel()
    {
        if (Volatile.Read(ref _disposed) == 1 || Interlocked.Exchange(ref _requested, 1) == 1)
        {
            return false;
        }

        try
        {
            _source.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>Kaydı kaldırır ve kaynağı bırakır (ikinci çağrı etkisiz).</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _onDispose(this);
        _source.Dispose();
    }
}
