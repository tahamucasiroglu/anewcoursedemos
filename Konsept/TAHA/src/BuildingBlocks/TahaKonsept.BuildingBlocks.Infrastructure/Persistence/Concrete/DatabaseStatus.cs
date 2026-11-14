using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IDatabaseStatus"/> uygulaması (tekil, iş parçacığı güvenli). Host'taki migration servisi
/// <see cref="MarkAttemptFailed"/> / <see cref="MarkReady"/> ile durumu günceller; okuyucular <see cref="IDatabaseStatus"/> üzerinden izler.
/// </summary>
public sealed class DatabaseStatus : IDatabaseStatus
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Lock _gate = new();
    private string? _lastError;
    private DateTimeOffset? _lastAttemptUtc;
    private int _attemptCount;

    /// <summary>Hazır mı.</summary>
    public bool IsReady => _ready.Task.IsCompleted;

    /// <summary>Son hata mesajı.</summary>
    public string? LastError
    {
        get
        {
            lock (_gate)
            {
                return _lastError;
            }
        }
    }

    /// <summary>Son deneme zamanı (UTC).</summary>
    public DateTimeOffset? LastAttemptUtc
    {
        get
        {
            lock (_gate)
            {
                return _lastAttemptUtc;
            }
        }
    }

    /// <summary>Deneme sayısı.</summary>
    public int AttemptCount
    {
        get
        {
            lock (_gate)
            {
                return _attemptCount;
            }
        }
    }

    /// <summary>Hazır olana kadar bekler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default) => _ready.Task.WaitAsync(cancellationToken);

    /// <summary>Başarılı denemeyi kaydeder ve bekleyenleri serbest bırakır.</summary>
    /// <param name="attemptUtc">Deneme zamanı (UTC).</param>
    public void MarkReady(DateTimeOffset attemptUtc)
    {
        lock (_gate)
        {
            _attemptCount++;
            _lastAttemptUtc = attemptUtc;
            _lastError = null;
        }

        _ready.TrySetResult();
    }

    /// <summary>Başarısız denemeyi kaydeder.</summary>
    /// <param name="attemptUtc">Deneme zamanı (UTC).</param>
    /// <param name="error">Hata mesajı.</param>
    public void MarkAttemptFailed(DateTimeOffset attemptUtc, string error)
    {
        lock (_gate)
        {
            _attemptCount++;
            _lastAttemptUtc = attemptUtc;
            _lastError = error;
        }
    }
}
