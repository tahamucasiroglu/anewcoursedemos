using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Auth.Concrete;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>Tek uçuşlu refresh koordinatörü (plan 0002 Risk 4; review R1-1, R1-3).</summary>
public sealed class TokenRefreshCoordinatorTests
{
    /// <summary>
    /// İlk istek (lider) refresh sürerken iptal edilse de Api çağrısı tamamlanır ve sonuç paylaşılır: aynı eski token'la gelen
    /// ikinci istek yeni çifti alır, Api'ye ikinci kez gidilmez (dönmüş token kaybolmaz).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Refresh_LeaderRequestAborted_SecondCallerStillGetsNewPair()
    {
        var api = Substitute.For<IKonseptApiClient>();
        var release = new TaskCompletionSource();
        var pair = new AuthSessionResponse("jwt-new", DateTimeOffset.UtcNow.AddMinutes(15), "knr_new", Guid.NewGuid(), "A");
        api.RefreshAsync("knr_old", Arg.Any<CancellationToken>()).Returns(async call =>
        {
            await release.Task.WaitAsync(call.Arg<CancellationToken>());
            return Result<AuthSessionResponse>.Success(pair);
        });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var coordinator = new TokenRefreshCoordinator(cache);
        using var aborted = new CancellationTokenSource();

        var leader = coordinator.RefreshAsync(api, "knr_old", aborted.Token);
        await aborted.CancelAsync();
        var follower = coordinator.RefreshAsync(api, "knr_old", TestContext.Current.CancellationToken);
        release.SetResult();
        await Should.NotThrowAsync(async () => await leader);
        var second = await follower;

        second.Value.RefreshToken.ShouldBe("knr_new");
        await api.Received(1).RefreshAsync("knr_old", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Lider geçici hatayla düşer, kuyruktaki çağıran dönüşü yaparken yeni bir çağıran gelir: ikinci eşzamanlı dönüş başlamaz
    /// (eski token Api'de 401 verirdi), iki çağıran da yeni çifti alır ve önbellekte başarı kalır (review R2-1).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Refresh_TransientLeaderFailure_NoSecondRotation_SuccessWins()
    {
        var ct = TestContext.Current.CancellationToken;
        var api = Substitute.For<IKonseptApiClient>();
        var leaderGo = new TaskCompletionSource();
        var secondStarted = new TaskCompletionSource();
        var secondGo = new TaskCompletionSource();
        var pair = new AuthSessionResponse("jwt-new", DateTimeOffset.UtcNow.AddMinutes(15), "knr_new", Guid.NewGuid(), "A");
        var calls = 0;
        api.RefreshAsync("knr_old", Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            switch (Interlocked.Increment(ref calls))
            {
                case 1:
                    await leaderGo.Task;
                    return Result<AuthSessionResponse>.Failure(Konsept.Web.ApiClient.ApiClientErrors.Unavailable);
                case 2:
                    secondStarted.SetResult();
                    await secondGo.Task;
                    return Result<AuthSessionResponse>.Success(pair);
                default:
                    return Result<AuthSessionResponse>.Failure(new("identity.session_invalid", "x", Konsept.Shared.Kernel.Enums.ErrorType.Unauthorized));
            }
        });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var coordinator = new TokenRefreshCoordinator(cache);

        var leader = coordinator.RefreshAsync(api, "knr_old", ct);
        var queued = coordinator.RefreshAsync(api, "knr_old", ct);
        leaderGo.SetResult();
        (await leader).IsSuccess.ShouldBeFalse();
        await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        var late = coordinator.RefreshAsync(api, "knr_old", ct);
        secondGo.SetResult();
        var queuedResult = await queued;
        var lateResult = await late;
        var cached = await coordinator.RefreshAsync(api, "knr_old", ct);

        queuedResult.Value.RefreshToken.ShouldBe("knr_new");
        lateResult.IsSuccess.ShouldBeTrue();
        cached.Value.RefreshToken.ShouldBe("knr_new");
        calls.ShouldBe(2);
    }

    /// <summary>Biten her uçuşun kilidi bırakılır; çok sayıda farklı token sözlüğü büyütmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Refresh_ManyTokens_GatesPruned()
    {
        var api = Substitute.For<IKonseptApiClient>();
        api.RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthSessionResponse>.Success(new AuthSessionResponse("jwt", DateTimeOffset.UtcNow.AddMinutes(15), "knr_x", Guid.NewGuid(), "A")));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var coordinator = new TokenRefreshCoordinator(cache);

        for (var i = 0; i < 50; i++)
        {
            await coordinator.RefreshAsync(api, $"knr_{i}", TestContext.Current.CancellationToken);
        }

        coordinator.ActiveGateCount.ShouldBe(0);
    }
}
