using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TahaKonsept.BuildingBlocks.Api.Modules.Abstract;

/// <summary>
/// Host'taki modül kayıt defteri: modülleri kaydeder, controller derlemelerini MVC'ye ekler, migration'ları ve uç eşlemelerini sırayla çalıştırır.
/// </summary>
public interface IModuleRegistry
{
    /// <summary>Kayıtlı modüller (ekleme sırasıyla).</summary>
    IReadOnlyList<IModule> Modules { get; }

    /// <summary>Tüm modüllerin servislerini kaydeder ve controller derlemelerini MVC'ye ekler.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    /// <param name="mvcBuilder">MVC oluşturucu (<c>AddControllers()</c> sonucu).</param>
    void RegisterServices(IServiceCollection services, IConfiguration configuration, IMvcBuilder mvcBuilder);

    /// <summary>Tüm modüllerin migration'larını sırayla uygular.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default);

    /// <summary>Tüm modüllerin ek uçlarını eşler.</summary>
    /// <param name="endpoints">Uç oluşturucu.</param>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
