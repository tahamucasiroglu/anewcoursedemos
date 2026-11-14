using System.Collections.Concurrent;
using TahaKonsept.Agent.Core.Execution.Abstract;

namespace TahaKonsept.Agent.Core.Execution.Base;

/// <summary>
/// <see cref="IWorkspaceUsage"/> ortak davranışı: alan başına iş parçacığı güvenli sayaç; sıfıra inen kayıt silinir.
/// </summary>
public abstract class WorkspaceUsageBase : IWorkspaceUsage
{
    private readonly ConcurrentDictionary<Guid, int> _counts = new();
    private readonly object _gate = new();

    /// <summary>Kullanımı başlatır.</summary>
    /// <param name="workspaceId">Çalışma alanı.</param>
    public void Enter(Guid workspaceId)
    {
        lock (_gate)
        {
            _counts[workspaceId] = _counts.TryGetValue(workspaceId, out var count) ? count + 1 : 1;
        }
    }

    /// <summary>Kullanımı bırakır.</summary>
    /// <param name="workspaceId">Çalışma alanı.</param>
    public void Exit(Guid workspaceId)
    {
        lock (_gate)
        {
            if (!_counts.TryGetValue(workspaceId, out var count) || count <= 1)
            {
                _counts.TryRemove(workspaceId, out _);
                return;
            }

            _counts[workspaceId] = count - 1;
        }
    }

    /// <summary>Kullanımda mı.</summary>
    /// <param name="workspaceId">Çalışma alanı.</param>
    /// <returns>Sonuç.</returns>
    public bool IsInUse(Guid workspaceId) => _counts.ContainsKey(workspaceId);
}
