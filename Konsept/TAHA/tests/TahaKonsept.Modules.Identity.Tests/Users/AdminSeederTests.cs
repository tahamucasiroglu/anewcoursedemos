using Microsoft.AspNetCore.Identity;
using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Modules.Identity.Tests.Support;
using TahaKonsept.Shared.Contracts.Identity;
using Xunit;

namespace TahaKonsept.Modules.Identity.Tests.Users;

/// <summary>
/// Yönetici tohumu (sahte e-posta + test içinde üretilen sahte özet): yoksa Admin rolünde oluşturur ve özeti olduğu gibi saklar;
/// ikinci çalıştırma dokunmaz (idempotent); var olan kullanıcının özeti ezilmez; ayar yoksa / özet bozuksa atlar;
/// yönetici normal giriş sayfasından e-postayla (ve kullanıcı adıyla) giriş yapabilir.
/// </summary>
public sealed class AdminSeederTests
{
    private const string Email = "yonetici@example.test";
    private const string DummyPassword = "Sahte-Parola-123";

    private static readonly string DummyHash = new PasswordHasher<AppUser>().HashPassword(new AppUser(), DummyPassword);

    /// <summary>Kullanıcı yoksa Admin rolünde oluşturulur, özet aynen saklanır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Seeds_admin_with_hash_stored_verbatim()
    {
        await using var host = new IdentityTestHost(new AdminSeedOptions { Email = Email, PasswordHash = DummyHash });

        (await host.RunAsync<IAdminSeeder, bool>(s => s.SeedAsync())).ShouldBeTrue();

        var stored = await host.RunAsync<UserManager<AppUser>, (string? Hash, string? Name, bool IsAdmin, string? Stamp)>(async m =>
        {
            var user = (await m.FindByEmailAsync(Email))!;
            return (user.PasswordHash, user.UserName, await m.IsInRoleAsync(user, RoleNames.Admin), user.SecurityStamp);
        });
        stored.Hash.ShouldBe(DummyHash);
        stored.Name.ShouldBe(Email);
        stored.IsAdmin.ShouldBeTrue();
        stored.Stamp.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>İkinci çalıştırma yeni kullanıcı açmaz ve mevcut özeti değiştirmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Seeding_is_idempotent_and_never_overwrites()
    {
        await using var host = new IdentityTestHost(new AdminSeedOptions { Email = Email, PasswordHash = DummyHash });
        await host.RunAsync<IAdminSeeder, bool>(s => s.SeedAsync());
        var otherHash = new PasswordHasher<AppUser>().HashPassword(new AppUser(), "Baska-Parola-456");
        await host.RunAsync<UserManager<AppUser>, IdentityResult>(async m =>
        {
            var user = (await m.FindByEmailAsync(Email))!;
            user.PasswordHash = otherHash;
            return await m.UpdateAsync(user);
        });

        (await host.RunAsync<IAdminSeeder, bool>(s => s.SeedAsync())).ShouldBeFalse();

        (await host.RunAsync<IUserDirectory, int>(d => d.CountAsync())).ShouldBe(1);
        (await host.RunAsync<UserManager<AppUser>, string?>(async m => (await m.FindByEmailAsync(Email))!.PasswordHash)).ShouldBe(otherHash);
    }

    /// <summary>Ayar yoksa ya da özet biçimi bozuksa hiçbir şey oluşturulmaz.</summary>
    /// <param name="email">E-posta.</param>
    /// <param name="hash">Özet.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData(null, null)]
    [InlineData(Email, null)]
    [InlineData(null, "AQAAAA==")]
    [InlineData(Email, "duz-metin-parola")]
    [InlineData(Email, "AQAAAA==")]
    public async Task Skips_when_not_configured_or_hash_invalid(string? email, string? hash)
    {
        await using var host = new IdentityTestHost(new AdminSeedOptions { Email = email, PasswordHash = hash });

        (await host.RunAsync<IAdminSeeder, bool>(s => s.SeedAsync())).ShouldBeFalse();

        (await host.RunAsync<IUserDirectory, int>(d => d.CountAsync())).ShouldBe(0);
    }

    /// <summary>Tohumlanan yönetici e-postayla ve kullanıcı adıyla giriş yapar; hatalı parola reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Seeded_admin_logs_in_with_email()
    {
        await using var host = new IdentityTestHost(new AdminSeedOptions { Email = Email, PasswordHash = DummyHash });
        await host.RunAsync<IAdminSeeder, bool>(s => s.SeedAsync());

        (await Login(host, Email, DummyPassword)).IsSuccess.ShouldBeTrue();
        (await Login(host, "YONETICI@example.test", DummyPassword)).IsSuccess.ShouldBeTrue();
        (await Login(host, Email, "yanlis-parola")).IsFailure.ShouldBeTrue();

        var users = await host.RunAsync<IUserDirectory, IReadOnlyList<UserSummaryDto>>(d => d.ListAsync());
        users.Single().IsAdmin.ShouldBeTrue();
    }

    /// <summary>Kayıt olan kullanıcı yönetici e-postasını kullanıcı adı olarak alamaz (ad zaten dolu).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Registration_cannot_take_seeded_admin_name()
    {
        await using var host = new IdentityTestHost(new AdminSeedOptions { Email = Email, PasswordHash = DummyHash });
        await host.RunAsync<IAdminSeeder, bool>(s => s.SeedAsync());

        (await host.RegisterAsync(Email)).IsFailure.ShouldBeTrue();
    }

    private static Task<Result<TokenResponse>> Login(IdentityTestHost host, string login, string password) =>
        host.RunAsync<IAuthService, Result<TokenResponse>>(s => s.LoginAsync(new LoginRequest { UserName = login, Password = password }));
}
