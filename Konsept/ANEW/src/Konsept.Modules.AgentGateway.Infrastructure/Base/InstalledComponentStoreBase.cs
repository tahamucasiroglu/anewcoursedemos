using System.Collections.Concurrent;
using System.Collections.Frozen;
using Konsept.Modules.AgentGateway.Application.Abstract;

namespace Konsept.Modules.AgentGateway.Infrastructure.Base;

/// <summary>
/// Kurulu bileşenler bellekte, kullanıcı başına (plan 0006 Risk 6): Api yeniden başlarsa ajan yeniden bağlanınca yine bildirir. Kimlikler
/// kırpılır, boşlar atılır; en çok 64 kimlik, her biri en çok 64 karakter.
/// </summary>
public abstract class InstalledComponentStoreBase : IInstalledComponentStore
{
    private const int MaxItems = 64;
    private const int MaxLength = 64;
    private readonly ConcurrentDictionary<Guid, FrozenSet<string>> _byUser = new();

    /// <inheritdoc />
    public IReadOnlySet<string> Get(Guid userId) =>
        _byUser.TryGetValue(userId, out var set) ? set : FrozenSet<string>.Empty;

    /// <inheritdoc />
    public void Set(Guid userId, IEnumerable<string> components)
    {
        ArgumentNullException.ThrowIfNull(components);
        _byUser[userId] = components
            .Select(c => c?.Trim() ?? string.Empty)
            .Where(c => c.Length is > 0 and <= MaxLength)
            .Take(MaxItems)
            .ToFrozenSet(StringComparer.Ordinal);
    }
}
