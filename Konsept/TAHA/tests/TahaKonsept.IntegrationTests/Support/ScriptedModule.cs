using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TahaKonsept.BuildingBlocks.Api.Modules.Base;

namespace TahaKonsept.IntegrationTests.Support;

/// <summary>
/// Test modülü: ilk <see cref="FailuresBeforeSuccess"/> migration denemesinde veritabanı hatası taklit eder, sonra başarılı olur.
/// <see cref="Attempts"/> deneme sayısını tutar.
/// </summary>
public sealed class ScriptedModule : ModuleBase
{
    private int _attempts;

    /// <summary>Başarıdan önceki başarısız deneme sayısı (<see cref="int.MaxValue"/> = hep başarısız).</summary>
    public int FailuresBeforeSuccess { get; init; }

    /// <summary>Yapılan deneme sayısı.</summary>
    public int Attempts => Volatile.Read(ref _attempts);

    /// <summary>Modül adı.</summary>
    public override string Name => "Scripted";

    /// <summary>Servis kaydı yok.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    /// <summary>Senaryoya göre hata fırlatır ya da başarılı olur.</summary>
    /// <param name="services">Servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        return attempt <= FailuresBeforeSuccess
            ? Task.FromException(new InvalidOperationException("Taklit: veritabanına ulaşılamadı."))
            : Task.CompletedTask;
    }
}
