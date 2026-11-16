using System.Net;
using System.Net.Http.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Profiles.Infrastructure.Persistence;
using Konsept.Modules.Profiles.Infrastructure.Persistence.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Profiles, gerçek PostgreSQL 17 ile (full profil): depo her işlemde kullanıcıyla süzer (AC-15 — kasıtlı bozma bu testi kırar),
/// tr-TR ad tekilliği veritabanı indeksinde de vardır, seçim kalıcıdır (AC-11).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ProfilesIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ProfilesIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public ProfilesIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// AC-15 (kasıtlı bozma hedefi): gerçek depo yalnızca istenen kullanıcının profillerini getirir, günceller ve siler; B'nin
    /// A'nın kimliğiyle yaptığı her işlem boş/başarısız döner ve A'nın profili değişmez; Api 404 döner.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Repository_Ownership_RealDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        var emailA = AuthenticatedClient.NewEmail();
        var emailB = AuthenticatedClient.NewEmail();
        using var a = await AuthenticatedClient.CreateAsync(_factory, ct, emailA);
        using var b = await AuthenticatedClient.CreateAsync(_factory, ct, emailB);
        var profileA = await ProfileApiTests.CreateAsync(a, "A Kanalı", ct);
        await ProfileApiTests.CreateAsync(b, "B Kanalı", ct);
        var idA = await UserIdAsync(emailA);
        var idB = await UserIdAsync(emailB);

        using var steal = await b.PutAsJsonAsync($"/api/profiles/{profileA}", ProfileApiTests.Body("Çalındı"), ct);
        using var remove = await b.DeleteAsync(new Uri($"/api/profiles/{profileA}", UriKind.Relative), ct);
        await using var scope = _factory.Services.CreateAsyncScope();
        var repository = new ProfileRepository(scope.ServiceProvider.GetRequiredService<ProfilesDbContext>());

        steal.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        remove.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await repository.ListAsync(idA, ct)).ShouldHaveSingleItem().Name.ShouldBe("A Kanalı");
        (await repository.ListAsync(idB, ct)).ShouldHaveSingleItem().Name.ShouldBe("B Kanalı");
        (await repository.GetAsync(idB, profileA, ct)).ShouldBeNull();
        (await repository.DeleteAsync(idB, profileA, ct)).ShouldBeFalse();
        (await repository.ListAsync(idA, ct)).Single().Id.ShouldBe(profileA);
    }

    /// <summary>AC-7/AC-11: "İstanbul" ile "istanbul" veritabanında da çakışır; seçim yeniden okununca aynıdır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TurkishUniqueness_AndSelection_RealDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct);
        var first = await ProfileApiTests.CreateAsync(client, "İstanbul", ct);
        await ProfileApiTests.CreateAsync(client, "Ankara", ct);

        using var duplicate = await client.PostAsJsonAsync("/api/profiles", ProfileApiTests.Body("istanbul"), ct);
        using var select = await client.PutAsJsonAsync("/api/profiles/selected", new { profileId = first }, ct);
        var list = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/profiles", ct);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        select.StatusCode.ShouldBe(HttpStatusCode.OK);
        list.GetProperty("selectedId").GetGuid().ShouldBe(first);
        list.GetProperty("items").GetArrayLength().ShouldBe(2);
    }

    private async Task<Guid> UserIdAsync(string email) =>
        (Guid)(await _factory.Database.ScalarAsync($"SELECT id FROM identity.users WHERE email = '{email}'"))!;
}
