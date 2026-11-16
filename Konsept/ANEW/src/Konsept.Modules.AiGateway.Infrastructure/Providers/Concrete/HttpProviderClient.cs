using Konsept.Modules.AiGateway.Infrastructure.Providers.Base;

namespace Konsept.Modules.AiGateway.Infrastructure.Providers.Concrete;

/// <summary>Sağlayıcılara tek deneme gönderen ham HTTP istemcisi (<see cref="HttpProviderClientBase"/>).</summary>
/// <param name="http">HTTP istemci fabrikası.</param>
public sealed class HttpProviderClient(IHttpClientFactory http) : HttpProviderClientBase(http);
