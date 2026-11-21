using System.Globalization;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OkulYonetim.Tests.Integration.Infrastructure;

/// <summary>
/// Test veritabanı yaşam döngüsü (docs/testing.md, spec 0001 AC-10): koşu başında (assembly fixture) artakalan
/// (1 saatten eski) okul_test_* veritabanlarını siler, kendine ait okul_test_&lt;zaman&gt;_&lt;guid&gt; veritabanını açar; bitince siler.
/// Eşzamanlı başka bir koşunun yeni veritabanına dokunmaz. Sunucu bağlantısı repoda değil,
/// user-secrets veya ortam değişkenindeki "ConnectionStrings:TestServer" ayarından gelir. Geliştirme veritabanına dokunmaz.
/// Bağlantı havuzu açık (uygulamanın kendi ayarı gibi; 0010 VERIFY V-7: havuzsuz her sorgu yeni fiziksel bağlantı açıyor,
/// hız ölçümü test ortamını ölçüyordu); bir veritabanı silinmeden önce havuzlar boşaltılır.
/// </summary>
public sealed class TestDatabase : IAsyncLifetime
{
    public const string NamePrefix = "okul_test_";

    public static readonly TimeSpan LeftoverAge = TimeSpan.FromHours(1);
    private const string StampFormat = "yyyyMMddHHmmss";

    public TestDatabase()
    {
        this.Name = NewName(DateTime.UtcNow);
        this.ConnectionString = new NpgsqlConnectionStringBuilder(ServerConnectionString) { Database = this.Name }.ConnectionString;
    }

    public static string ServerConnectionString { get; } = LoadServerConnectionString();

    public string Name { get; }

    public string ConnectionString { get; }

    public async ValueTask InitializeAsync()
    {
        await RemoveLeftoversAsync(except: null).ConfigureAwait(false);
        await CreateAsync(this.Name).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() => await DropAsync(this.Name).ConfigureAwait(false);

    public static async Task<bool> ExistsAsync(string name)
    {
        await using NpgsqlConnection connection = new(ServerConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using NpgsqlCommand command = new("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        command.Parameters.AddWithValue("name", name);
        return await command.ExecuteScalarAsync().ConfigureAwait(false) is not null;
    }

    /// <summary>
    /// Veritabanını siler. Önce havuzlar boşaltılır: havuzda bekleyen boşta bağlantılar silmeyi engellemesin ve silinen
    /// veritabanına ait (FORCE ile kesilmiş) bağlantılar havuzda kalıp sonradan kullanılmasın.
    /// </summary>
    public static async Task DropAsync(string name)
    {
        NpgsqlConnection.ClearAllPools();
        await ExecuteOnServerAsync($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)").ConfigureAwait(false);
    }

    public static async Task CreateAsync(string name) =>
        await ExecuteOnServerAsync($"CREATE DATABASE \"{name}\"").ConfigureAwait(false);

    public static async Task RemoveLeftoversAsync(string? except)
    {
        List<string> leftovers = [];
        await using (NpgsqlConnection connection = new(ServerConnectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            await using NpgsqlCommand command = new("SELECT datname FROM pg_database WHERE datname LIKE 'okul\\_test\\_%'", connection);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                leftovers.Add(reader.GetString(0));
            }
        }

        foreach (string name in leftovers.Where(n => n != except && IsStale(n, DateTime.UtcNow)))
        {
            await DropAsync(name).ConfigureAwait(false);
        }
    }

    /// <summary>Adı oluşturma zamanını (UTC) taşır: okul_test_yyyyMMddHHmmss_guid.</summary>
    public static string NewName(DateTime utc) =>
        NamePrefix + utc.ToString(StampFormat, CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("N");

    /// <summary>
    /// Artık mı? Yalnızca 1 saatten eski (veya zaman damgası okunamayan eski biçimli) test veritabanları artıktır;
    /// eşzamanlı başka bir koşunun yeni veritabanına dokunulmaz.
    /// </summary>
    public static bool IsStale(string name, DateTime utcNow)
    {
        string rest = name.Length > NamePrefix.Length ? name[NamePrefix.Length..] : string.Empty;
        string stamp = rest.Length >= StampFormat.Length ? rest[..StampFormat.Length] : rest;
        return !DateTime.TryParseExact(stamp, StampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime created)
            || utcNow - created > LeftoverAge;
    }

    private static async Task ExecuteOnServerAsync(string sql)
    {
        await using NpgsqlConnection connection = new(ServerConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static string LoadServerConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<TestDatabase>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        string? value = configuration.GetConnectionString("TestServer");
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Test veritabanı sunucusu ayarı yok: 'dotnet user-secrets set ConnectionStrings:TestServer \"Host=localhost;Port=5432;Username=...;Password=...\"' (tests/Integration) veya ConnectionStrings__TestServer ortam değişkeni.");
        }

        return new NpgsqlConnectionStringBuilder(value) { Database = "postgres", Pooling = true }.ConnectionString;
    }
}
