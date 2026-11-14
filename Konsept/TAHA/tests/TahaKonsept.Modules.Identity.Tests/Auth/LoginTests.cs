using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
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
/// Giriş: doğru parola → doğrulanabilir JWT (sub, name, permission dizisi, role, tfam, client=web) + refresh token;
/// hatalı parola ve bilinmeyen kullanıcı → aynı Unauthorized hatası.
/// </summary>
public sealed class LoginTests
{
    /// <summary>Doğru bilgilerle giriş, aynı anahtarla doğrulanan JWT üretir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_with_valid_credentials_returns_verifiable_jwt()
    {
        await using var host = new IdentityTestHost();
        var (_, userId) = await host.SetupAdminAsync();

        var result = await host.RunAsync<IAuthService, Result<TokenResponse>>(
            s => s.LoginAsync(new LoginRequest { UserName = " taha ", Password = IdentityTestHost.Password }));

        result.IsSuccess.ShouldBeTrue();
        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(result.Value.AccessToken, new TokenValidationParameters
        {
            ValidIssuer = host.JwtOptions.Issuer,
            ValidAudience = host.JwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(host.SigningKey),
        });
        validation.IsValid.ShouldBeTrue(validation.Exception?.Message);

        var jwt = new JsonWebToken(result.Value.AccessToken);
        jwt.GetClaim(CustomClaimTypes.UserId).Value.ShouldBe(userId.ToString());
        jwt.GetClaim(CustomClaimTypes.UserName).Value.ShouldBe(IdentityTestHost.AdminUserName);
        jwt.GetClaim(CustomClaimTypes.ClientType).Value.ShouldBe("web");
        jwt.GetClaim("role").Value.ShouldBe(RoleNames.Admin);
        jwt.Claims.Where(c => c.Type == CustomClaimTypes.Permission).Select(c => c.Value).ShouldBe(PermissionNames.All, ignoreOrder: true);

        using var payload = JsonDocument.Parse(Base64UrlEncoder.Decode(result.Value.AccessToken.Split('.')[1]));
        payload.RootElement.GetProperty(CustomClaimTypes.Permission).ValueKind.ShouldBe(JsonValueKind.Array);
    }

    /// <summary>Hatalı parola reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_with_wrong_password_is_unauthorized()
    {
        await using var host = new IdentityTestHost();
        await host.SetupAdminAsync();

        var result = await host.RunAsync<IAuthService, Result<TokenResponse>>(
            s => s.LoginAsync(new LoginRequest { UserName = IdentityTestHost.AdminUserName, Password = "yanlis-parola" }));

        result.Error.ShouldBe(IdentityErrors.InvalidCredentials);
    }

    /// <summary>Bilinmeyen kullanıcı aynı hatayı alır (kullanıcı adı sızdırılmaz).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_with_unknown_user_is_unauthorized()
    {
        await using var host = new IdentityTestHost();
        await host.SetupAdminAsync();

        var result = await host.RunAsync<IAuthService, Result<TokenResponse>>(
            s => s.LoginAsync(new LoginRequest { UserName = "kimse", Password = IdentityTestHost.Password }));

        result.Error.ShouldBe(IdentityErrors.InvalidCredentials);
    }
}
