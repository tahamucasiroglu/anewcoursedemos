using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Ports;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>
/// Sıraya sokulmuş işlem (giriş ve yenilemenin temeli; review H-1, M-1, I-4): aynı anahtar ikinci isteği bekletir ve en
/// fazla 5 sn sonra <see cref="ExclusiveLockTimeoutException"/> ile bırakır; farklı anahtar beklemez; commit'siz kapanış
/// kilidi bırakır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ExclusiveScopeTests
{
    private readonly TestDatabase database;

    public ExclusiveScopeTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task BeginExclusive_SameKeyHeld_TimesOutAfterAboutFiveSeconds_OtherKeyDoesNotWait_ReleaseOnDispose()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        string key = "test:" + Guid.NewGuid().ToString("N");
        CancellationToken ct = TestContext.Current.CancellationToken;
        using IServiceScope holderScope = factory.Services.CreateScope();
        using IServiceScope waiterScope = factory.Services.CreateScope();
        IUnitOfWork holder = holderScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IUnitOfWork waiter = waiterScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IExclusiveScope held = await holder.BeginExclusiveAsync(key, ct);
        Stopwatch watch = Stopwatch.StartNew();
        await Should.ThrowAsync<ExclusiveLockTimeoutException>(() => waiter.BeginExclusiveAsync(key, ct));
        watch.Elapsed.ShouldBeInRange(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(15));

        watch.Restart();
        await using (await waiter.BeginExclusiveAsync("test:" + Guid.NewGuid().ToString("N"), ct))
        {
            watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
        }

        await held.DisposeAsync();
        watch.Restart();
        await using (await waiter.BeginExclusiveAsync(key, ct))
        {
            watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
        }
    }
}
