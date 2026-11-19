using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OkulYonetim.Tests.Integration.Infrastructure;

/// <summary>
/// Test veritabanı yaşam döngüsü (docs/testing.md, spec 0001 AC-10): koşu başında (assembly fixture) artakalan
/// okul_test_* veritabanlarını siler, kendine ait okul_test_&lt;guid&gt; veritabanını açar; bitince siler. Sunucu bağlantısı repoda değil,
/// user-secrets veya ortam değişkenindeki "ConnectionStrings:TestServer" ayarından gelir. Geliştirme veritabanına dokunmaz.
/// </summary>
public sealed class TestDatabase : IAsyncLifetime
{
    public const string NamePrefix = "okul_test_";

    public TestDatabase()
    {
        this.Name = NamePrefix + Guid.NewGuid().ToString("N");
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

    public async ValueTask DisposeAsync()
    {
        await ExecuteOnServerAsync($"DROP DATABASE IF EXISTS \"{this.Name}\" WITH (FORCE)").ConfigureAwait(false);
    }

    public static async Task<bool> ExistsAsync(string name)
    {
        await using NpgsqlConnection connection = new(ServerConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using NpgsqlCommand command = new("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        command.Parameters.AddWithValue("name", name);
        return await command.ExecuteScalarAsync().ConfigureAwait(false) is not null;
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

        foreach (string name in leftovers.Where(n => n != except))
        {
            await ExecuteOnServerAsync($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)").ConfigureAwait(false);
        }
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

        return new NpgsqlConnectionStringBuilder(value) { Database = "postgres", Pooling = false }.ConnectionString;
    }
}
