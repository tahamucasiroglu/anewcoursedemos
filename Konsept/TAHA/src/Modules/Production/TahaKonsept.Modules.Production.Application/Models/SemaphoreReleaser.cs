namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Kullanıcı kilidini dispose edilince bir kez bırakan yardımcı (<c>IUserJobLock</c> sonucu).
/// </summary>
public sealed class SemaphoreReleaser : IDisposable
{
    private SemaphoreSlim? _semaphore;

    /// <summary>Oluşturur.</summary>
    /// <param name="semaphore">Alınmış semafor.</param>
    public SemaphoreReleaser(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
    }

    /// <summary>Kilidi bırakır (ikinci çağrı etkisiz).</summary>
    public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
}
