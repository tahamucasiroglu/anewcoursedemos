using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Modules.Identity.Tests.Support;
using TahaKonsept.Shared.Contracts.Identity;
using Xunit;

namespace TahaKonsept.Modules.Identity.Tests.Auth;

/// <summary>
/// Refresh rotasyonu: her yenileme yeni token verir (aynı aile); kullanılmış token tekrar gelirse aile tamamen iptal edilir;
/// süresi dolan token reddedilir.
/// </summary>
public sealed class RefreshRotationTests
{
    /// <summary>Yenileme yeni ve farklı bir refresh token verir; aile korunur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Refresh_rotates_token_within_same_family()
    {
        await using var host = new IdentityTestHost();
        var (first, _) = await host.SetupAdminAsync();

        var second = await Refresh(host, first.RefreshToken);

        second.IsSuccess.ShouldBeTrue();
        second.Value.RefreshToken.ShouldNotBe(first.RefreshToken);
        Family(second.Value).ShouldBe(Family(first));

        var third = await Refresh(host, second.Value.RefreshToken);
        third.IsSuccess.ShouldBeTrue();
        Family(third.Value).ShouldBe(Family(first));
    }

    /// <summary>Kullanılmış token tekrar gelince aile iptal edilir: en yeni token da artık çalışmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Reusing_rotated_token_revokes_whole_family()
    {
        await using var host = new IdentityTestHost();
        var (first, userId) = await host.SetupAdminAsync();
        var second = (await Refresh(host, first.RefreshToken)).Value;

        var reuse = await Refresh(host, first.RefreshToken);
        reuse.Error.ShouldBe(IdentityErrors.RefreshTokenReuse);

        var latest = await Refresh(host, second.RefreshToken);
        latest.IsFailure.ShouldBeTrue();
        latest.Error.Type.ShouldBe(ErrorType.Unauthorized);

        host.Notifier.FamiliesRevoked.ShouldContain((userId, Guid.Parse(Family(first))));
    }

    /// <summary>Tekrar kullanım yalnızca o aileyi etkiler; başka girişin ailesi çalışmaya devam eder.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Reuse_detection_does_not_touch_other_families()
    {
        await using var host = new IdentityTestHost();
        var (first, _) = await host.SetupAdminAsync();
        var other = (await host.RunAsync<IAuthService, Result<TokenResponse>>(
            s => s.LoginAsync(new LoginRequest { UserName = IdentityTestHost.AdminUserName, Password = IdentityTestHost.Password }))).Value;

        await Refresh(host, first.RefreshToken);
        (await Refresh(host, first.RefreshToken)).IsFailure.ShouldBeTrue();

        (await Refresh(host, other.RefreshToken)).IsSuccess.ShouldBeTrue();
    }

    /// <summary>Süresi dolmuş refresh token reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        await using var host = new IdentityTestHost();
        var (first, _) = await host.SetupAdminAsync();
        host.Clock.Advance(TimeSpan.FromDays(host.JwtOptions.RefreshTokenDays + 1));

        (await Refresh(host, first.RefreshToken)).Error.ShouldBe(IdentityErrors.InvalidRefreshToken);
    }

    /// <summary>Bilinmeyen token reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Unknown_refresh_token_is_rejected()
    {
        await using var host = new IdentityTestHost();
        await host.SetupAdminAsync();

        (await Refresh(host, "uydurma-token")).Error.ShouldBe(IdentityErrors.InvalidRefreshToken);
    }

    private static Task<Result<TokenResponse>> Refresh(IdentityTestHost host, string refreshToken) =>
        host.RunAsync<IAuthService, Result<TokenResponse>>(s => s.RefreshAsync(new RefreshRequest { RefreshToken = refreshToken }));

    private static string Family(TokenResponse tokens) => new JsonWebToken(tokens.AccessToken).GetClaim(CustomClaimTypes.TokenFamily).Value;
}
