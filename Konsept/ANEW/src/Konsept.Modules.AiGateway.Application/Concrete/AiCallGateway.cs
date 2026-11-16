using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Base;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.AiGateway.Application.Concrete;

/// <summary>Çağrı kapısı.</summary>
/// <param name="catalog">Katalog.</param>
/// <param name="keys">Key deposu.</param>
/// <param name="protector">Key çözme.</param>
/// <param name="provider">Sağlayıcı istemcisi.</param>
/// <param name="spend">Harcama deposu.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Log.</param>
public sealed class AiCallGateway(
    ICatalogSource catalog,
    IApiKeyRepository keys,
    IApiKeyProtector protector,
    IProviderClient provider,
    ISpendRepository spend,
    TimeProvider time,
    ILogger<AiCallGateway> logger) : AiCallGatewayBase(catalog, keys, protector, provider, spend, time, logger);
