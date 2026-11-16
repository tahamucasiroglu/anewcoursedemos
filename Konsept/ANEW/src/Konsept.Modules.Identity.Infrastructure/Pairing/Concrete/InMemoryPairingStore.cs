using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Infrastructure.Pairing.Base;

namespace Konsept.Modules.Identity.Infrastructure.Pairing.Concrete;

/// <summary>
/// Kodları bellekte tutar (Pi'de tek Api süreci). Api yeniden başlarsa bekleyen kodlar kaybolur; ajan "süresi doldu" görüp
/// kendiliğinden yeni kod alır (plan Risk 2).
/// </summary>
public sealed class InMemoryPairingStore : PairingStoreBase
{
    private readonly Dictionary<string, PairingRequest> _byCode = new(StringComparer.Ordinal);

    /// <inheritdoc />
    protected override PairingRequest? FindByCode(string code) => _byCode.GetValueOrDefault(code);

    /// <inheritdoc />
    protected override PairingRequest? FindBySecretHash(string secretHash) =>
        _byCode.Values.FirstOrDefault(r => string.Equals(r.SecretHash, secretHash, StringComparison.Ordinal));

    /// <inheritdoc />
    protected override void Save(PairingRequest request) => _byCode[request.Code] = request;

    /// <inheritdoc />
    protected override void Purge(DateTimeOffset expiredBefore)
    {
        foreach (var code in _byCode.Values.Where(r => r.ExpiresAt < expiredBefore).Select(r => r.Code).ToList())
        {
            _byCode.Remove(code);
        }
    }
}
