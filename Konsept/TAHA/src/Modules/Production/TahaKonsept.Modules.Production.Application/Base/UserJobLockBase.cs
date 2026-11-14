using System.Collections.Concurrent;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Models;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IUserJobLock"/> ortak davranışı: kullanıcı başına bir <see cref="SemaphoreSlim"/> (tek süreç; Pi'de tek API örneği).
/// </summary>
public abstract class UserJobLockBase : IUserJobLock
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    /// <summary>Kilidi alır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bırakıcı.</returns>
    public async Task<IDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken)
    {
        var semaphore = _locks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new SemaphoreReleaser(semaphore);
    }

    /// <summary>Kilit boştaysa hemen alır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Bırakıcı ya da <c>null</c>.</returns>
    public IDisposable? TryAcquire(Guid userId)
    {
        var semaphore = _locks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        return semaphore.Wait(0) ? new SemaphoreReleaser(semaphore) : null;
    }
}
