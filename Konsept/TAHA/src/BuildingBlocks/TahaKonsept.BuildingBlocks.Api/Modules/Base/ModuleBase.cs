using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TahaKonsept.BuildingBlocks.Api.Modules.Abstract;

namespace TahaKonsept.BuildingBlocks.Api.Modules.Base;

/// <summary>
/// <see cref="IModule"/> için ortak taban: <see cref="MigrateAsync"/> ve <see cref="MapEndpoints"/> varsayılan olarak boştur.
/// DbContext'i olan modül: <c>public override Task MigrateAsync(IServiceProvider sp, CancellationToken ct) =&gt; MigrateDbContextAsync&lt;ConceptsDbContext&gt;(sp, ct);</c>
/// </summary>
public abstract class ModuleBase : IModule
{
    /// <summary>Modül adı.</summary>
    public abstract string Name { get; }

    /// <summary>Modül servislerini kaydeder.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public abstract void RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Migration'ları uygular (varsayılan: hiçbir şey).</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>Ek uçları eşler (varsayılan: hiçbir şey).</summary>
    /// <param name="endpoints">Uç oluşturucu.</param>
    public virtual void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    /// <summary>Verilen DbContext'in migration'larını ayrı bir scope'ta uygular.</summary>
    /// <typeparam name="TContext">DbContext türü.</typeparam>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected static async Task MigrateDbContextAsync<TContext>(IServiceProvider services, CancellationToken cancellationToken)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
