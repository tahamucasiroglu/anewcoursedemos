using Konsept.Modules.Production.Infrastructure.Pipeline.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Production.Infrastructure.Pipeline.Concrete;

/// <summary>Üretim koşucusu (spec 0017); hat <c>Production:Pipeline:Enabled</c> ayarıyla kapatılabilir (varsayılan açık).</summary>
/// <param name="scopes">Kapsam fabrikası.</param>
/// <param name="lifetime">Uygulama yaşam döngüsü.</param>
/// <param name="configuration">Yapılandırma.</param>
/// <param name="logger">Teknik log.</param>
public sealed class ProductionRunner(IServiceScopeFactory scopes, IHostApplicationLifetime lifetime, IConfiguration configuration, ILogger<ProductionRunner> logger)
    : ProductionRunnerBase(scopes, lifetime, configuration.GetValue("Production:Pipeline:Enabled", true), logger);
