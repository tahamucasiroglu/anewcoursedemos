using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using TahaKonsept.Api.Database;
using TahaKonsept.BuildingBlocks.Api.Modules.Concrete;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Concrete;
using TahaKonsept.BuildingBlocks.Infrastructure.Time.Concrete;
using TahaKonsept.IntegrationTests.Support;

namespace TahaKonsept.IntegrationTests.Api;

/// <summary>
/// Başlangıç migration servisinin dayanıklılık testleri: veritabanı yokken API çökmez, durum "hazır değil" kalır ve yeniden dener.
/// </summary>
public sealed class ModuleMigrationHostedServiceTests
{
    /// <summary>İlk denemeler başarısız olsa da servis yeniden dener ve sonunda hazır olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task RetriesUntilDatabaseBecomesAvailable()
    {
        var ct = TestContext.Current.CancellationToken;
        var module = new ScriptedModule { FailuresBeforeSuccess = 2 };
        var status = new DatabaseStatus();
        using var service = Create(module, status);

        await service.StartAsync(ct);
        await status.WaitUntilReadyAsync(ct).WaitAsync(TimeSpan.FromSeconds(15), ct);
        await service.StopAsync(ct);

        status.IsReady.ShouldBeTrue();
        status.LastError.ShouldBeNull();
        module.Attempts.ShouldBe(3);
    }

    /// <summary>Veritabanı hiç gelmezse servis istisna fırlatmaz; durum hatayı taşır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task StaysUnavailableWithoutThrowing_WhenDatabaseNeverComes()
    {
        var ct = TestContext.Current.CancellationToken;
        var module = new ScriptedModule { FailuresBeforeSuccess = int.MaxValue };
        var status = new DatabaseStatus();
        using var service = Create(module, status);

        await service.StartAsync(ct);
        while (module.Attempts < 2)
        {
            await Task.Delay(50, ct);
        }

        await service.StopAsync(ct);

        status.IsReady.ShouldBeFalse();
        status.LastError.ShouldNotBeNull();
        status.AttemptCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    private static ModuleMigrationHostedService Create(ScriptedModule module, DatabaseStatus status)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:RetrySeconds"] = "1",
                ["Database:MaxRetrySeconds"] = "1",
                ["ConnectionStrings:Main"] = "Host=localhost;Port=1;Database=test;Username=test",
            })
            .Build();
        var services = new ServiceCollection().BuildServiceProvider();
        return new ModuleMigrationHostedService(
            services,
            new ModuleRegistry([module]),
            status,
            new SystemClock(),
            configuration,
            NullLogger<ModuleMigrationHostedService>.Instance);
    }
}
