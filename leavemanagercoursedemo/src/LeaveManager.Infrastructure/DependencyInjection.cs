using LeaveManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LeaveManager.Infrastructure;

/// <summary>Registers and initializes infrastructure; the only entry point Web uses.</summary>
public static class DependencyInjection
{
    private const string ConnectionStringName = "LeaveManager";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        connectionString = connectionString.Replace(
            "|ContentRoot|", environment.ContentRootPath, StringComparison.Ordinal);

        services.AddDbContext<LeaveManagerDbContext>(options => options.UseSqlite(connectionString));
        return services;
    }

    /// <summary>
    /// Creates the database when it does not exist. Uses EnsureCreated until the first
    /// migration exists (plan 0001, risk 5); spec 0002 switches to Migrate().
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LeaveManagerDbContext>();

        var dataSource = context.Database.GetDbConnection().DataSource;
        var isFile = !string.IsNullOrEmpty(dataSource)
            && !string.Equals(dataSource, ":memory:", StringComparison.Ordinal);
        if (isFile && Path.GetDirectoryName(dataSource) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }
}
