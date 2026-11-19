using Microsoft.EntityFrameworkCore;

namespace OkulYonetim.Persistence.Contexts;

/// <summary>
/// Uygulamanın tek veritabanı bağlamı (PostgreSQL). İskelette henüz iş tablosu yoktur; tablolar ve
/// kiracı filtresi ilk iş varlıklarıyla eklenir. İsimlendirme snake_case'tir (kayıtta ayarlanır).
/// </summary>
public sealed class OkulDbContext : DbContext
{
    public OkulDbContext(DbContextOptions<OkulDbContext> options)
        : base(options)
    {
    }
}
