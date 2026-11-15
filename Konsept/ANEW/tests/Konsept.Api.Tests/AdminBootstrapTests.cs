using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// İlk admin (spec 0011 AC-4, hızlı profil): ayardaki e-postayla (başka harf biçimiyle, tr-TR) kayıt olan admin olur, başka
/// e-posta kullanıcı kalır; açılışta var olan hesap admin olur, ayar boşsa kimse admin kalmaz; rol değişikliği yeniden giriş
/// gerekmeden bir sonraki istekte geçerlidir.
/// </summary>
public sealed class AdminBootstrapTests
{
    private const string OverviewPath = "/api/admin/overview";

    private static readonly TimeSpan SyncLimit = TimeSpan.FromSeconds(10);

    /// <summary>Ayardaki e-posta başka harf biçimiyle yazılarak kayıt olunsa da hesap admin olur; başka e-posta kullanıcı kalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_AdminEmailOtherCase_GetsAdmin_OtherEmailStaysUser()
    {
        var ct = TestContext.Current.CancellationToken;
        CultureInfo.CurrentCulture.Name.ShouldBe("tr-TR");
        await using var factory = new IdentityApiFactory();

        using var admin = await AuthenticatedClient.CreateAsync(factory, ct, "ADMIN@Ornek.Test");
        using var other = await AuthenticatedClient.CreateAsync(factory, ct, "baska@ornek.test");
        using var adminOverview = await admin.GetAsync(new Uri(OverviewPath, UriKind.Relative), ct);
        using var otherOverview = await other.GetAsync(new Uri(OverviewPath, UriKind.Relative), ct);

        factory.Users.Get("admin@ornek.test").Role.ShouldBe(Role.Admin);
        factory.Users.Get("baska@ornek.test").Role.ShouldBe(Role.User);
        adminOverview.StatusCode.ShouldBe(HttpStatusCode.OK);
        otherOverview.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Api açılışında ayardaki e-postaya ait hesap zaten varsa admin olur; ayar tek doğruluk kaynağıdır, başka bir admin
    /// kullanıcıya döner (plan Risk 2).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Startup_ExistingAdminEmailAccount_Promoted()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        var existing = User.Register("Var Olan", "Admin@Ornek.Test", "HASH", factory.Time.GetUtcNow());
        var former = User.Register("Eski Admin", "eski@ornek.test", "HASH", factory.Time.GetUtcNow(), Role.Admin);
        await factory.Users.TryAddAsync(existing, ct);
        await factory.Users.TryAddAsync(former, ct);

        using var client = factory.CreateClient();
        await Eventually.WithinAsync(() => Task.FromResult(factory.Users.SyncCalls > 0), SyncLimit, ct);

        existing.Role.ShouldBe(Role.Admin);
        former.Role.ShouldBe(Role.User);
    }

    /// <summary>Ayar boşken açılışta kimse admin kalmaz ve ayardaki e-postanın kendisiyle kayıt olan da kullanıcıdır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Startup_EmptySetting_NobodyAdmin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        var preset = User.Register("Önceden Admin", IdentityApiFactory.AdminEmail, "HASH", factory.Time.GetUtcNow(), Role.Admin);
        await factory.Users.TryAddAsync(preset, ct);
        await using var empty = factory.WithWebHostBuilder(b => b.UseSetting("Identity:AdminEmail", string.Empty));

        using var client = await AuthenticatedClient.CreateAsync(empty, ct, "yeni@ornek.test");
        await Eventually.WithinAsync(() => Task.FromResult(factory.Users.SyncCalls > 0), SyncLimit, ct);
        using var overview = await client.GetAsync(new Uri(OverviewPath, UriKind.Relative), ct);

        preset.Role.ShouldBe(Role.User);
        factory.Users.Get("yeni@ornek.test").Role.ShouldBe(Role.User);
        factory.Users.Users.ShouldAllBe(u => u.Role == Role.User);
        overview.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Rol değişikliği (senkron) aynı token'la bir sonraki istekte geçerlidir: yükseltilen kullanıcı yeniden giriş yapmadan
    /// admin ucuna erişir, geri alınınca yine 403 alır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task RoleChange_AppliesNextRequest_NoRelogin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(factory, ct, email);
        await Eventually.WithinAsync(() => Task.FromResult(factory.Users.SyncCalls > 0), SyncLimit, ct);

        using var before = await client.GetAsync(new Uri(OverviewPath, UriKind.Relative), ct);
        await factory.Users.SyncAdminRoleAsync(EmailNormalizer.Normalize(email), ct);
        using var promoted = await client.GetAsync(new Uri(OverviewPath, UriKind.Relative), ct);
        var session = await client.GetFromJsonAsync<JsonElement>("/api/auth/session", ct);
        await factory.Users.SyncAdminRoleAsync(null, ct);
        using var demoted = await client.GetAsync(new Uri(OverviewPath, UriKind.Relative), ct);

        before.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        promoted.StatusCode.ShouldBe(HttpStatusCode.OK);
        session.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ShouldBe(["admin.panel"]);
        demoted.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
