using System.Security.Cryptography;
using Konsept.Modules.Concepts.Contracts.Abstract;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Bellek içi Yeniden üret bağlamı (spec 0020): kullanıcı + konsept başına 128 bit rastgele belirteç, <see cref="Lifetime"/> sonra geçersiz.
/// Karşılaştırma sabit zamanlıdır. Tekil (singleton) kaydedilir.
/// </summary>
/// <param name="time">Saat.</param>
public abstract class ConceptRegenerationContextBase(TimeProvider time) : IConceptRegenerationContext
{
    /// <summary>Bağlamın ömrü.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(2);

    private readonly Lock _gate = new();
    private readonly Dictionary<(Guid UserId, Guid ConceptId), (string Token, DateTimeOffset Expires)> _open = [];

    /// <inheritdoc />
    public string Open(Guid userId, Guid conceptId)
    {
        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        lock (_gate)
        {
            _open[(userId, conceptId)] = (token, time.GetUtcNow() + Lifetime);
        }

        return token;
    }

    /// <inheritdoc />
    public bool IsOpen(Guid userId, Guid conceptId, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        lock (_gate)
        {
            return _open.TryGetValue((userId, conceptId), out var entry)
                && entry.Expires > time.GetUtcNow()
                && CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(entry.Token), System.Text.Encoding.UTF8.GetBytes(token));
        }
    }

    /// <inheritdoc />
    public void Close(Guid userId, Guid conceptId)
    {
        lock (_gate)
        {
            _open.Remove((userId, conceptId));
        }
    }
}
