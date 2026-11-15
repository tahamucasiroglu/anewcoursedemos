using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;
using Konsept.Modules.Identity.Infrastructure.Security;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Roller ve admin ucu, hızlı profil (spec 0011 AC-1, AC-2, AC-3, AC-5 Api tarafı): yeni hesap kullanıcıdır, yetkisiz
/// kullanıcı admin ucundan 403 alır ve veri dönmez, admin boş iskeleti görür; kayıt isteğine eklenen rol alanları ve
/// token'a eklenen sahte yetki claim'i hiçbir şey değiştirmez.
/// </summary>
public sealed class RoleApiTests : IClassFixture<IdentityApiFactory>
{
    private const string OverviewPath = "/api/admin/overview";

    private readonly IdentityApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="RoleApiTests"/> class.</summary>
    /// <param name="factory">Kimlik test fabrikası (ilk admin e-postası <c>admin@ornek.test</c>).</param>
    public RoleApiTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Kayıt formuyla açılan yeni hesap kullanıcı rolündedir ve <c>admin.panel</c> yetkisi yoktur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_NewAccount_RoleUser_NoAdminPanelPermission()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);

        var session = await client.GetFromJsonAsync<JsonElement>("/api/auth/session", ct);

        _factory.Users.Get(email).Role.ShouldBe(Role.User);
        session.GetProperty("permissions").EnumerateArray().ShouldBeEmpty();
    }

    /// <summary>
    /// Kullanıcı rolündeki biri admin ucunu doğrudan isteyince 403 alır ve gövdede hiçbir admin verisi yoktur; politika adı ve
    /// gerektirdiği yetki Identity'nin <c>admin.panel</c> yetkisiyle aynıdır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminEndpoint_UserRole_Returns403WithoutData()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct);

        using var response = await client.GetAsync(new Uri(OverviewPath, UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        body.ShouldNotContain("sections", Case.Insensitive);
        AuthPolicies.AdminPanel.ShouldBe(Permissions.AdminPanel);
    }

    /// <summary>Admin rolündeki kullanıcı admin ucundan boş iskeleti alır ve oturum yanıtında <c>admin.panel</c> görünür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminEndpoint_AdminRole_ReturnsEmptySkeleton()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await _factory.CreateAdminClientAsync(ct);

        var overview = await admin.GetFromJsonAsync<JsonElement>(OverviewPath, ct);
        var session = await admin.GetFromJsonAsync<JsonElement>("/api/auth/session", ct);

        overview.GetProperty("sections").EnumerateArray().ShouldBeEmpty();
        session.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ShouldBe(["admin.panel"]);
    }

    /// <summary>
    /// Kayıt isteğine rol/yetki alanı eklemek (farklı adlarla) hiçbir şey değiştirmez: hesap kullanıcı rolünde kalır, yetkisi
    /// yoktur ve admin ucundan 403 alır.
    /// </summary>
    /// <param name="field">Eklenen alan adı.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("role")]
    [InlineData("Role")]
    [InlineData("permissions")]
    [InlineData("isAdmin")]
    public async Task Register_WithRoleFields_StaysUser(string field)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = AuthenticatedClient.NewEmail();
        object value = field switch
        {
            "permissions" => new[] { "admin.panel" },
            "isAdmin" => true,
            _ => "admin",
        };
        var body = new Dictionary<string, object> { ["name"] = "Sinsi", ["email"] = email, ["password"] = AuthenticatedClient.Password, [field] = value };

        using var register = await AuthenticatedClient.PostAsync(client, _factory.Services, "/api/auth/register", body, ct);
        var session = await register.Content.ReadFromJsonAsync<JsonElement>(ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        using var overview = await client.GetAsync(new Uri(OverviewPath, UriKind.Relative), ct);
        var current = await client.GetFromJsonAsync<JsonElement>("/api/auth/session", ct);

        register.StatusCode.ShouldBe(HttpStatusCode.OK);
        _factory.Users.Get(email).Role.ShouldBe(Role.User);
        overview.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        current.GetProperty("permissions").EnumerateArray().ShouldBeEmpty();
    }

    /// <summary>
    /// Geçerli imzalı ve geçerli oturumlu bir token'a <c>konsept.perm=admin.panel</c> claim'i eklenmiş olsa bile kullanıcı
    /// rolündeki biri admin ucundan 403 alır: yetki token'dan değil, her istekte veritabanındaki rolden kurulur.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Admin_ForgedPermissionClaimInJwt_StillForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = AuthenticatedClient.NewEmail();
        var session = await AuthenticatedClient.RegisterAsync(client, _factory.Services, email, AuthenticatedClient.Password, "Sahte", ct);
        var genuine = new JsonWebToken(session.GetProperty("accessToken").GetString());
        var now = _factory.Time.GetUtcNow().UtcDateTime;
        var forged = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = JwtSettings.Issuer,
            Audience = JwtSettings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(10),
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["sub"] = genuine.GetClaim("sub").Value,
                ["sid"] = genuine.GetClaim("sid").Value,
                ["name"] = "Sahte",
                [AuthPolicies.PermissionClaim] = Permissions.AdminPanel,
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(FastApiFactory.SigningKey)), SecurityAlgorithms.HmacSha256),
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        using var overview = await client.GetAsync(new Uri(OverviewPath, UriKind.Relative), ct);
        using var me = await client.GetAsync(new Uri("/api/auth/session", UriKind.Relative), ct);
        var current = await me.Content.ReadFromJsonAsync<JsonElement>(ct);

        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        overview.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        current.GetProperty("permissions").EnumerateArray().ShouldBeEmpty();
        _factory.Users.Get(email).Role.ShouldBe(Role.User);
    }
}
