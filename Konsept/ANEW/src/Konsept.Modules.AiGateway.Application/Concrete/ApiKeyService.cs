using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Base;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.AiGateway.Application.Concrete;

/// <summary>API key servisi.</summary>
/// <param name="catalog">Katalog.</param>
/// <param name="keys">Key deposu.</param>
/// <param name="protector">Şifreleme.</param>
/// <param name="validator">Sağlayıcı doğrulaması.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Log.</param>
public sealed class ApiKeyService(
    ICatalogSource catalog,
    IApiKeyRepository keys,
    IApiKeyProtector protector,
    IProviderKeyValidator validator,
    ICurrentUser currentUser,
    TimeProvider time,
    ILogger<ApiKeyService> logger) : ApiKeyServiceBase(catalog, keys, protector, validator, currentUser, time, logger);
