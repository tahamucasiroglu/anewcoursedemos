using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TahaKonsept.BuildingBlocks.Api.Modules.Abstract;

/// <summary>
/// Modül sözleşmesi: her modülün Api projesinde bir <c>&lt;Modül&gt;Module</c> sınıfı (genelde <c>ModuleBase</c>'den) bunu uygular
/// ve host'taki modül listesine eklenir. Modülün controller'ları aynı derlemede olduğu için otomatik keşfedilir.
/// Önemli üyeler: <see cref="RegisterServices"/>, <see cref="MigrateAsync"/>, <see cref="MapEndpoints"/> (ör. SignalR hub'ı).
/// </summary>
public interface IModule
{
    /// <summary>Modül adı (ör. <c>Concepts</c>).</summary>
    string Name { get; }

    /// <summary>Modülün servislerini (DbContext, repository, servis, validator) kaydeder.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Modülün veritabanı migration'larını uygular (host başlarken sırayla çağrılır).</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default);

    /// <summary>Controller dışı uçları eşler (ör. <c>endpoints.MapHub&lt;AgentHub&gt;("/hubs/agent")</c>).</summary>
    /// <param name="endpoints">Uç oluşturucu.</param>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
