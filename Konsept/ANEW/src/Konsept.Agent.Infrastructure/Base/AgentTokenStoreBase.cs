using Konsept.Agent.Application.Abstract;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Ajan oturum belirteci deposunun ortak mantığı (spec 0012 Req 9): boş belirteç saklanmaz, okunan değer kırpılır. Saklama
/// biçimi alt sınıftadır.
/// </summary>
public abstract class AgentTokenStoreBase : IAgentTokenStore
{
    /// <inheritdoc />
    public string? Read()
    {
        var raw = Load()?.Trim();
        return string.IsNullOrEmpty(raw) ? null : raw;
    }

    /// <inheritdoc />
    public void Save(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Store(token.Trim());
    }

    /// <inheritdoc />
    public void Clear() => Delete();

    /// <summary>Saklı değeri okur.</summary>
    /// <returns>Değer ya da <c>null</c>.</returns>
    protected abstract string? Load();

    /// <summary>Değeri saklar.</summary>
    /// <param name="token">Belirteç.</param>
    protected abstract void Store(string token);

    /// <summary>Saklı değeri siler.</summary>
    protected abstract void Delete();
}
