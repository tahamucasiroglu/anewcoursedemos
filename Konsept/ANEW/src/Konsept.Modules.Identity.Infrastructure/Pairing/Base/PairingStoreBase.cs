using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Entities;

namespace Konsept.Modules.Identity.Infrastructure.Pairing.Base;

/// <summary>
/// Kod deposunun kilitli ortak mantığı (spec 0012, plan Risk 2): her işlem tek kilit altında çalışır; eklemede süresi bir kod
/// ömrü kadar önce dolmuş istekler atılır. Saklama biçimi alt sınıftadır.
/// </summary>
public abstract class PairingStoreBase : IPairingStore
{
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public bool Contains(string code)
    {
        lock (_gate)
        {
            return FindByCode(code) is not null;
        }
    }

    /// <inheritdoc />
    public void Add(PairingRequest request, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_gate)
        {
            Purge(now - (request.ExpiresAt - request.CreatedAt));
            Save(request);
        }
    }

    /// <inheritdoc />
    public T WithCode<T>(string code, Func<PairingRequest?, T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (_gate)
        {
            return action(FindByCode(code));
        }
    }

    /// <inheritdoc />
    public T WithSecretHash<T>(string secretHash, Func<PairingRequest?, T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (_gate)
        {
            return action(FindBySecretHash(secretHash));
        }
    }

    /// <summary>Koda göre bulur (kilit altında çağrılır).</summary>
    /// <param name="code">Kod.</param>
    /// <returns>İstek ya da <c>null</c>.</returns>
    protected abstract PairingRequest? FindByCode(string code);

    /// <summary>Sır hash'ine göre bulur (kilit altında çağrılır).</summary>
    /// <param name="secretHash">Hash.</param>
    /// <returns>İstek ya da <c>null</c>.</returns>
    protected abstract PairingRequest? FindBySecretHash(string secretHash);

    /// <summary>Saklar (kilit altında çağrılır).</summary>
    /// <param name="request">İstek.</param>
    protected abstract void Save(PairingRequest request);

    /// <summary>Verilen zamandan önce süresi dolmuş istekleri atar (kilit altında çağrılır).</summary>
    /// <param name="expiredBefore">Sınır.</param>
    protected abstract void Purge(DateTimeOffset expiredBefore);
}
