using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Modül DbContext'leri için ortak EF ayarı: Npgsql, migration geçmiş tablosu modülün kendi şemasında
/// (<see cref="MigrationsHistoryTable"/>), migration derlemesi = DbContext'in derlemesi, snake_case adlandırma.
/// Hem çalışma zamanı kaydı hem design-time factory bunu kullanır.
/// </summary>
public static class ModuleDbContextOptionsConfigurator
{
    /// <summary>Migration geçmiş tablosu adı.</summary>
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    /// <summary>Seçenek oluşturucuyu yapılandırır.</summary>
    /// <param name="builder">Seçenek oluşturucu.</param>
    /// <param name="connectionString">Bağlantı metni.</param>
    /// <param name="schema">Modül şeması (ör. <c>concepts</c>).</param>
    /// <param name="migrationsAssembly">Migration'ların bulunduğu derleme.</param>
    /// <returns>Aynı oluşturucu.</returns>
    public static DbContextOptionsBuilder Configure(
        DbContextOptionsBuilder builder,
        string connectionString,
        string schema,
        Assembly migrationsAssembly)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentNullException.ThrowIfNull(migrationsAssembly);

        builder
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable(MigrationsHistoryTable, schema);
                npgsql.MigrationsAssembly(migrationsAssembly.GetName().Name);
            })
            .UseSnakeCaseNamingConvention();
        return builder;
    }
}
