using System.Collections.Concurrent;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Models;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IJobCancellationRegistry"/> ortak davranışı: (kullanıcı, öğe) başına tek etkin <see cref="JobCancellationScope"/> (bellek içi; Pi'de tek API örneği).
/// Aynı öğe için yeni kapsam açılırsa eskisinin yerini alır; eski kapsamın dispose'u yenisini silmez.
/// </summary>
public abstract class JobCancellationRegistryBase : IJobCancellationRegistry
{
    private readonly ConcurrentDictionary<(Guid UserId, Guid QueueItemId), JobCancellationScope> _scopes = new();

    /// <summary>Kapsam açar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="queueItemId">Sıra öğesi.</param>
    /// <param name="cancellationToken">Üst belirteç.</param>
    /// <returns>Kapsam.</returns>
    public IJobCancellationScope Begin(Guid userId, Guid queueItemId, CancellationToken cancellationToken)
    {
        var scope = new JobCancellationScope((userId, queueItemId), cancellationToken, Remove);
        _scopes[scope.Key] = scope;
        return scope;
    }

    /// <summary>İptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="queueItemId">Sıra öğesi.</param>
    /// <returns>Sonuç.</returns>
    public bool Cancel(Guid userId, Guid queueItemId) => _scopes.TryGetValue((userId, queueItemId), out var scope) && scope.RequestCancel();

    private void Remove(JobCancellationScope scope) =>
        _scopes.TryRemove(new KeyValuePair<(Guid UserId, Guid QueueItemId), JobCancellationScope>(scope.Key, scope));
}
