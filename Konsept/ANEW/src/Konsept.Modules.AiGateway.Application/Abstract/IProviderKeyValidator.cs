using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>
/// Key'i sağlayıcının maliyetsiz ucunda doğrular (spec 0004 Req 10): tek deneme, tekrar yok, en fazla seed'deki süre (10 sn),
/// token harcayan uç hiç çağrılmaz. Hata fırlatmaz; sonuçlanmayan her durum <see cref="KeyCheckOutcome.Inconclusive"/>'dır.
/// </summary>
public interface IProviderKeyValidator
{
    /// <summary>Key'i doğrular.</summary>
    /// <param name="provider">Sağlayıcı (doğrulama ucu seed'den).</param>
    /// <param name="key">Kırpılmış key.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<KeyCheckOutcome> CheckAsync(ProviderEntry provider, string key, CancellationToken cancellationToken);
}
