using Npgsql;
using Xunit;

namespace Konsept.Testing;

/// <summary>
/// Entegrasyon testleri için temiz bir PostgreSQL veritabanı: her fixture örneği
/// <c>konsept_test_&lt;guid&gt;</c> adlı yeni bir veritabanı açar ve bitince siler (docs/testing.md,
/// Resource hygiene kural 5). Sunucu adresi <c>KONSEPT_TEST_PG</c> ortam değişkeninden gelir;
/// <c>scripts/test-db/run-integration.sh</c> bunu geçici bir cluster için ayarlar (plan 0001, R-1).
/// </summary>
public sealed class PostgresTestDatabase : IAsyncLifetime
{
    /// <summary>Sunucu bağlantı metnini taşıyan ortam değişkeni.</summary>
    public const string ServerVariable = "KONSEPT_TEST_PG";

    private readonly string _databaseName = $"konsept_test_{Guid.NewGuid():N}";
    private string? _serverConnectionString;

    /// <summary>Gets test veritabanının bağlantı metni.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _serverConnectionString = Environment.GetEnvironmentVariable(ServerVariable)
            ?? throw new InvalidOperationException(
                $"{ServerVariable} tanımlı değil. Entegrasyon testleri scripts/test-db/run-integration.sh ile çalışır.");
        await ExecuteAsync($"CREATE DATABASE \"{_databaseName}\"").ConfigureAwait(false);
        ConnectionString = new NpgsqlConnectionStringBuilder(_serverConnectionString) { Database = _databaseName }.ConnectionString;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_serverConnectionString is not null)
        {
            NpgsqlConnection.ClearAllPools();
            await ExecuteAsync($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)").ConfigureAwait(false);
        }
    }

    /// <summary>Test veritabanında tek değer döndüren bir sorgu çalıştırır.</summary>
    /// <param name="sql">SQL.</param>
    /// <returns>İlk satırın ilk değeri.</returns>
    public async Task<object?> ScalarAsync(string sql)
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false))
            {
                return await command.ExecuteScalarAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteAsync(string sql)
    {
        var connection = new NpgsqlConnection(_serverConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false))
            {
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
