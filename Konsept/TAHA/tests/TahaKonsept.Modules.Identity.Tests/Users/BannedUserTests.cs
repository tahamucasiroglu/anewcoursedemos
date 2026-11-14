using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Tests.Support;
using TahaKonsept.Shared.Contracts.Identity;
using Xunit;

namespace TahaKonsept.Modules.Identity.Tests.Users;

/// <summary>
/// Yasak: yasaklı kullanıcı giriş yapamaz, token yenileyemez, ona token verilemez; yasak kaldırılınca giriş açılır;
/// son etkin yönetici yasaklanamaz; dizin listesi yasak/yönetici bilgisini taşır.
/// </summary>
public sealed class BannedUserTests
{
    private const string MemberName = "uye";

    /// <summary>Yasaklı kullanıcı giriş/yenileme/token alma yapamaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Banned_user_cannot_login_refresh_or_receive_tokens()
    {
        await using var host = new IdentityTestHost();
        await host.SetupAdminAsync();
        var memberId = await host.CreateUserAsync(MemberName);
        var before = (await Login(host, MemberName)).Value;

        var ban = await host.RunAsync<IUserDirectory, Result>(d => d.SetBannedAsync(memberId, banned: true));
        ban.IsSuccess.ShouldBeTrue();

        (await Login(host, MemberName)).Error.ShouldBe(IdentityErrors.UserBanned);
        (await host.RunAsync<IAuthService, Result<TokenResponse>>(s => s.RefreshAsync(new RefreshRequest { RefreshToken = before.RefreshToken })))
            .IsFailure.ShouldBeTrue();
        (await host.RunAsync<ITokenIssuer, Result<IssuedTokens>>(i => i.IssueAsync(memberId, TokenClientKind.Agent)))
            .Error.ShouldBe(IdentityErrors.UserBanned);
        host.Notifier.AllRevoked.ShouldContain(memberId);
    }

    /// <summary>Yasak kaldırılınca giriş tekrar çalışır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Unbanned_user_can_login_again()
    {
        await using var host = new IdentityTestHost();
        await host.SetupAdminAsync();
        var memberId = await host.CreateUserAsync(MemberName);
        await host.RunAsync<IUserDirectory, Result>(d => d.SetBannedAsync(memberId, banned: true));

        await host.RunAsync<IUserDirectory, Result>(d => d.SetBannedAsync(memberId, banned: false));

        (await Login(host, MemberName)).IsSuccess.ShouldBeTrue();
    }

    /// <summary>Son etkin yönetici yasaklanamaz (tek kullanıcı kendini kilitleyemez).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Last_active_admin_cannot_be_banned()
    {
        await using var host = new IdentityTestHost();
        var (_, adminId) = await host.SetupAdminAsync();

        var ban = await host.RunAsync<IUserDirectory, Result>(d => d.SetBannedAsync(adminId, banned: true));

        ban.Error.ShouldBe(IdentityErrors.LastAdmin);
        (await Login(host, IdentityTestHost.AdminUserName)).IsSuccess.ShouldBeTrue();
    }

    /// <summary>Dizin listesi yönetici ve yasak bayraklarını doğru gösterir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Directory_lists_users_with_flags()
    {
        await using var host = new IdentityTestHost();
        await host.SetupAdminAsync();
        var memberId = await host.CreateUserAsync(MemberName);
        await host.RunAsync<IUserDirectory, Result>(d => d.SetBannedAsync(memberId, banned: true));

        var users = await host.RunAsync<IUserDirectory, IReadOnlyList<UserSummaryDto>>(d => d.ListAsync());

        users.Count.ShouldBe(2);
        users.Single(u => u.UserName == IdentityTestHost.AdminUserName).IsAdmin.ShouldBeTrue();
        var member = users.Single(u => u.Id == memberId);
        member.IsAdmin.ShouldBeFalse();
        member.IsBanned.ShouldBeTrue();
    }

    private static Task<Result<TokenResponse>> Login(IdentityTestHost host, string userName) =>
        host.RunAsync<IAuthService, Result<TokenResponse>>(s => s.LoginAsync(new LoginRequest { UserName = userName, Password = IdentityTestHost.Password }));
}
