using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence;

/// <summary>
/// Veritabanı şema geçişlerini (migration) uygular. API katmanı veritabanı bağlamını tanımadan bunu çağırır
/// (yasak bağımlılık: API → DbContext); hata çağırana fırlatılır, çağıran loglar.
/// </summary>
public static class DatabaseMigration
{
    public static void MigrateOkulDatabase(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<OkulDbContext>().Database.Migrate();
    }
}
