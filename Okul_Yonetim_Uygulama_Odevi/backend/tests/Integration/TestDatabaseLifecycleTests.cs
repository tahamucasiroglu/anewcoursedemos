using Npgsql;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;

namespace OkulYonetim.Tests.Integration;

/// <summary>Test veritabanı yaşam döngüsünün testi (spec 0001 AC-10).</summary>
[Trait("Category", "Integration")]
public sealed class TestDatabaseLifecycleTests
{
    private readonly TestDatabase shared;

    public TestDatabaseLifecycleTests(TestDatabase shared)
    {
        this.shared = shared;
    }

    [Fact]
    public async Task Run_CreatesOwnDatabase_DropsItAndRemovesLeftovers()
    {
        // Bu koşunun veritabanı var ve geliştirme veritabanı değil
        (await TestDatabase.ExistsAsync(this.shared.Name)).ShouldBeTrue();
        this.shared.Name.ShouldStartWith(TestDatabase.NamePrefix);
        this.shared.Name.ShouldNotBe("okul_dev");

        // Şema geçişleri uygulanabiliyor (API başlangıçta uygular)
        using (OkulApiFactory factory = new(this.shared.ConnectionString))
        {
            using HttpClient client = factory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync(new Uri("/api/system-health", UriKind.Relative), TestContext.Current.CancellationToken);
        }

        (await CountMigrationsAsync(this.shared.ConnectionString)).ShouldBeGreaterThanOrEqualTo(1);

        // Açılan veritabanı bitince silinir
        TestDatabase own = new();
        await TestDatabase.CreateAsync(own.Name);
        (await TestDatabase.ExistsAsync(own.Name)).ShouldBeTrue();
        await own.DisposeAsync();
        (await TestDatabase.ExistsAsync(own.Name)).ShouldBeFalse();

        // Yarıda kalmış bir koşunun artığı temizlenir; bu koşununki kalır
        string leftover = TestDatabase.NamePrefix + "artik_" + Guid.NewGuid().ToString("N");
        await TestDatabase.CreateAsync(leftover);
        await TestDatabase.RemoveLeftoversAsync(except: this.shared.Name);
        (await TestDatabase.ExistsAsync(leftover)).ShouldBeFalse();
        (await TestDatabase.ExistsAsync(this.shared.Name)).ShouldBeTrue();
    }

    private static async Task<long> CountMigrationsAsync(string connectionString)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new("SELECT COUNT(*) FROM \"__EFMigrationsHistory\"", connection);
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
