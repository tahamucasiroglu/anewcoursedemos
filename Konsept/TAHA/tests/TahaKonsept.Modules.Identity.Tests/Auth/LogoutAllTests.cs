using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Tests.Support;
using TahaKonsept.Shared.Contracts.Identity;
using Xunit;

namespace TahaKonsept.Modules.Identity.Tests.Auth;

/// <summary>
/// Tüm oturumlardan çıkış: web ve ajan refresh token'larının hepsi iptal edilir, bildirimci çağrılır;
/// ajan ailesi tek başına da iptal edilebilir.
/// </summary>
public sealed class LogoutAllTests
{
    /// <summary>logout-all ajan token'larını da iptal eder ve bildirimciyi çağırır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Logout_all_revokes_web_and_agent_tokens_and_notifies()
    {
        await using var host = new IdentityTestHost();
        var (web, userId) = await host.SetupAdminAsync();
        var agent = await host.RunAsync<ITokenIssuer, Result<IssuedTokens>>(i => i.IssueAsync(userId, TokenClientKind.Agent));
        agent.IsSuccess.ShouldBeTrue();
        new JsonWebToken(agent.Value.Tokens.AccessToken).GetClaim(CustomClaimTypes.ClientType).Value.ShouldBe("agent");

        host.CurrentUser.UserId = userId;
        var logout = await host.RunAsync<IAuthService, Result>(s => s.LogoutAllAsync());
        logout.IsSuccess.ShouldBeTrue();

        (await Refresh(host, web.RefreshToken)).IsFailure.ShouldBeTrue();
        (await Refresh(host, agent.Value.Tokens.RefreshToken)).IsFailure.ShouldBeTrue();
        host.Notifier.AllRevoked.ShouldContain(userId);
    }

    /// <summary>Oturum yoksa logout-all Unauthorized döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Logout_all_without_session_is_unauthorized()
    {
        await using var host = new IdentityTestHost();
        await host.SetupAdminAsync();

        var logout = await host.RunAsync<IAuthService, Result>(s => s.LogoutAllAsync());

        logout.Error.ShouldBe(IdentityErrors.NotAuthenticated);
    }

    /// <summary>Tek ajan ailesinin iptali web oturumunu etkilemez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Revoking_agent_family_keeps_web_session()
    {
        await using var host = new IdentityTestHost();
        var (web, userId) = await host.SetupAdminAsync();
        var agent = (await host.RunAsync<ITokenIssuer, Result<IssuedTokens>>(i => i.IssueAsync(userId, TokenClientKind.Agent))).Value;

        var revoked = await host.RunAsync<ITokenRevoker, Result<int>>(r => r.RevokeFamilyAsync(userId, agent.FamilyId));

        revoked.Value.ShouldBe(1);
        (await Refresh(host, agent.Tokens.RefreshToken)).IsFailure.ShouldBeTrue();
        (await Refresh(host, web.RefreshToken)).IsSuccess.ShouldBeTrue();
        host.Notifier.FamiliesRevoked.ShouldContain((userId, agent.FamilyId));
    }

    private static Task<Result<TokenResponse>> Refresh(IdentityTestHost host, string refreshToken) =>
        host.RunAsync<IAuthService, Result<TokenResponse>>(s => s.RefreshAsync(new RefreshRequest { RefreshToken = refreshToken }));
}
