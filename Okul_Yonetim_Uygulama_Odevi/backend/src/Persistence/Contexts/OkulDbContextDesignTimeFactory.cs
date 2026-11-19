using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OkulYonetim.Persistence.Contexts;

/// <summary>
/// Yalnızca <c>dotnet ef migrations</c> aracı için: bağlamı veritabanına bağlanmadan, çalışma anındaki ayarlarla
/// (Npgsql + snake_case) kurar. Migration üretimi bağlantı açmaz; bağlantı cümlesi yer tutucudur.
/// </summary>
public sealed class OkulDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OkulDbContext>
{
    public OkulDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<OkulDbContext>()
            .UseNpgsql("Host=localhost;Database=okul_design_time")
            .UseSnakeCaseNamingConvention()
            .Options);
}
