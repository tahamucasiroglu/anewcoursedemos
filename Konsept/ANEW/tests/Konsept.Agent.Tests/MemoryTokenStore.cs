using Konsept.Agent.Application.Abstract;

namespace Konsept.Agent.Tests;

/// <summary>Testlerde bellekte ajan oturum belirteci deposu.</summary>
public sealed class MemoryTokenStore : IAgentTokenStore
{
    private string? _token;

    /// <inheritdoc />
    public string? Read() => Volatile.Read(ref _token);

    /// <inheritdoc />
    public void Save(string token) => Volatile.Write(ref _token, token);

    /// <inheritdoc />
    public void Clear() => Volatile.Write(ref _token, null);
}
