using Microsoft.Extensions.Configuration;
using Npgsql;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL bağlantı metnini çözer. Sıra: <c>TAHAKONSEPT_DB</c> ortam değişkeni (tam metin) →
/// <c>ConnectionStrings:Main</c>. Metinde parola yoksa <c>TAHAKONSEPT_DB_PASSWORD</c> ortam değişkeni eklenir.
/// </summary>
public static class DatabaseConnectionString
{
    /// <summary>Tam bağlantı metni ortam değişkeni.</summary>
    public const string ConnectionEnvironmentVariable = "TAHAKONSEPT_DB";

    /// <summary>Parola ortam değişkeni.</summary>
    public const string PasswordEnvironmentVariable = "TAHAKONSEPT_DB_PASSWORD";

    /// <summary>Bağlantı adı (<c>ConnectionStrings:Main</c>).</summary>
    public const string ConnectionName = "Main";

    /// <summary>Design-time (migration üretimi) varsayılan bağlantısı.</summary>
    public const string DesignTimeDefault = "Host=localhost;Port=5432;Database=tahakonsept;Username=tahakonsept;Password=dev";

    /// <summary>Yapılandırmadan bağlantı metnini çözer.</summary>
    /// <param name="configuration">Yapılandırma.</param>
    /// <returns>Bağlantı metni.</returns>
    /// <exception cref="InvalidOperationException">Hiçbir kaynakta bağlantı yoksa.</exception>
    public static string Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var raw = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = configuration.GetConnectionString(ConnectionName);
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                $"Veritabanı bağlantısı yok: ConnectionStrings:{ConnectionName} ya da {ConnectionEnvironmentVariable} tanımlayın.");
        }

        return ApplyPassword(raw);
    }

    /// <summary>Design-time bağlantısı: <c>TAHAKONSEPT_DB</c> ya da <see cref="DesignTimeDefault"/>.</summary>
    /// <returns>Bağlantı metni.</returns>
    public static string ResolveDesignTime()
    {
        var raw = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
        return ApplyPassword(string.IsNullOrWhiteSpace(raw) ? DesignTimeDefault : raw);
    }

    /// <summary>Loglar için parolasız kısa tanım üretir (ör. <c>localhost:5433/tahakonsept (kullanıcı tahakonsept)</c>).</summary>
    /// <param name="configuration">Yapılandırma.</param>
    /// <returns>Tanım; bağlantı tanımlı değilse açıklayıcı metin.</returns>
    public static string Describe(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(Resolve(configuration));
            var password = string.IsNullOrEmpty(builder.Password) ? "parola YOK" : "parola var";
            return $"{builder.Host}:{builder.Port}/{builder.Database} (kullanıcı {builder.Username}, {password})";
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return ex.Message;
        }
    }

    private static string ApplyPassword(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var password = Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);
        if (string.IsNullOrEmpty(builder.Password) && !string.IsNullOrEmpty(password))
        {
            builder.Password = password;
        }

        return builder.ConnectionString;
    }
}
