using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Account.AccountTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Account;

/// <summary>Spec 0006 AC-7, AC-8: bağlantı 30 dk ve tek kullanımlık; yeni bağlantı / şifre değişikliği eskileri geçersiz kılar; yenileme bütün oturumları kapatır ve kilidi açar.</summary>
[Trait("Category", "Integration")]
public sealed class ResetLinkTests
{
    private const string Invalid = "Bu bağlantı geçersiz ya da süresi dolmuş";
    private readonly TestDatabase database;

    public ResetLinkTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task ValidLink_ResetsPassword_ClosesAllSessions_UnlocksAccount_WorksOnce()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("yenile");
        Guid personId = await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        JsonElement session = await LoginAsync(client, person);
        for (int i = 0; i < 3; i++)
        {
            using HttpResponseMessage fail = await PostLoginAsync(client, person, "Yanlis-Sifre-" + i);
        }

        await ForgotAsync(client, person);
        string token = TokenFrom(outbox.To(person).Single());
        (await PostAnonymousAsync(client, "/api/auth/reset-password/check", new { token })).Raw.ShouldContain("true");

        var shortPassword = await ResetAsync(client, token, "kisa");
        shortPassword.Status.ShouldBe(HttpStatusCode.BadRequest);
        (await ResetAsync(client, token, NewPassword, NewPassword + "x")).Status.ShouldBe(HttpStatusCode.BadRequest);
        var done = await ResetAsync(client, token);

        done.Status.ShouldBe(HttpStatusCode.NoContent, done.Raw);
        (await StatusAsync(client, HttpMethod.Get, "/api/me", AccessToken(session))).ShouldBe(HttpStatusCode.Unauthorized);
        using (HttpResponseMessage old = await PostLoginAsync(client, person, Password))
        {
            old.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        (await LoginAsync(client, person, NewPassword)).GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();
        var again = await ResetAsync(client, token, "Bir-Daha-2026");
        again.Status.ShouldBe(HttpStatusCode.BadRequest);
        again.Raw.ShouldContain(Invalid);
        (await PostAnonymousAsync(client, "/api/auth/reset-password/check", new { token })).Raw.ShouldContain("false");
        string stored = await QueryAsync(factory, c => c.PasswordResetTokens.Where(t => t.UserId == personId).Select(t => t.TokenHash).SingleAsync());
        stored.ShouldNotBe(token);
        stored.ShouldNotContain(token);
    }

    [Fact]
    public async Task ExpiredReplacedChangedAndFakeLinks_AreRejected_PasswordUnchanged()
    {
        CapturingOutbox outbox = new();
        FakeTimeProvider time = new(DateTimeOffset.UtcNow);
        using OkulApiFactory factory = Factory(this.database, outbox, time);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("gecersiz");
        Guid personId = await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);

        await ForgotAsync(client, person);
        string expired = TokenFrom(outbox.To(person).Last());
        time.Advance(TimeSpan.FromMinutes(31));
        (await ResetAsync(client, expired)).Raw.ShouldContain(Invalid);

        await ForgotAsync(client, person);
        string replaced = TokenFrom(outbox.To(person).Last());
        await ForgotAsync(client, person);
        string latest = TokenFrom(outbox.To(person).Last());
        (await ResetAsync(client, replaced)).Raw.ShouldContain(Invalid);

        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{personId}/reset-password", founder)).ShouldBe(HttpStatusCode.OK);
        (await ResetAsync(client, latest)).Raw.ShouldContain(Invalid);

        time.Advance(TimeSpan.FromMinutes(16));
        await ForgotAsync(client, person);
        string beforeChange = TokenFrom(outbox.To(person).Last());
        string hashAfterAdminReset = await QueryAsync(factory, c => c.Users.Where(u => u.Id == personId).Select(u => u.PasswordHash).SingleAsync());
        (await ResetAsync(client, "uydurma-anahtar-0123456789")).Raw.ShouldContain(Invalid);
        (await QueryAsync(factory, c => c.Users.Where(u => u.Id == personId).Select(u => u.PasswordHash).SingleAsync())).ShouldBe(hashAfterAdminReset);
        (await ResetAsync(client, beforeChange)).Status.ShouldBe(HttpStatusCode.NoContent, "en son bağlantı hâlâ geçerli");
    }

    /// <summary>Review F-1: aynı bağlantıyla aynı anda iki yenileme — tam olarak biri başarılı, öbürü "geçersiz".</summary>
    [Fact]
    public async Task SameLinkUsedTwiceAtOnce_ExactlyOneSucceeds()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("cift-kullanim");
        await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        await ForgotAsync(client, person);
        string token = TokenFrom(outbox.To(person).Single());

        var results = await Task.WhenAll(ResetAsync(client, token, "Birinci-Sifre-2026"), ResetAsync(client, token, "Ikinci-Sifre-2026"));

        results.Count(r => r.Status == HttpStatusCode.NoContent).ShouldBe(1);
        results.Single(r => r.Status != HttpStatusCode.NoContent).Raw.ShouldContain(Invalid);
        string winner = results[0].Status == HttpStatusCode.NoContent ? "Birinci-Sifre-2026" : "Ikinci-Sifre-2026";
        (await LoginAsync(client, person, winner)).GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();
    }

    /// <summary>Review F-1: kurucu sıfırlamasıyla aynı anda kullanılan bağlantı ya önce çalışır ya da geçersiz olur; ikisi birden şifreyi değiştiremez.</summary>
    [Fact]
    public async Task LinkUsedWhileFounderResets_NeverBothWin()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("yaris");
        Guid personId = await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        await ForgotAsync(client, person);
        string token = TokenFrom(outbox.To(person).Single());

        Task<(HttpStatusCode Status, JsonElement Body, string Raw)> founderReset = CallAsync(client, HttpMethod.Post, $"/api/admin/members/{personId}/reset-password", founder);
        Task<(HttpStatusCode Status, string Raw)> linkReset = ResetAsync(client, token, "Bagla-Sifre-2026");
        await Task.WhenAll(founderReset, linkReset);

        (await founderReset).Status.ShouldBe(HttpStatusCode.OK);
        string temporary = (await founderReset).Body.GetProperty("temporaryPassword").GetString()!;
        using HttpResponseMessage withTemporary = await PostLoginAsync(client, person, temporary);
        using HttpResponseMessage withLink = await PostLoginAsync(client, person, "Bagla-Sifre-2026");
        // Kurucu sonra çalıştıysa onun şifresi geçerli; önce çalıştıysa bağlantı geçersizleşmiş olmalı. Her iki sırada da
        // son geçerli şifre kurucunun geçici şifresidir; bağlantının şifresi hiçbir zaman sonradan kazanamaz.
        withTemporary.StatusCode.ShouldBe(HttpStatusCode.OK);
        withLink.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        _ = await linkReset;
    }

    [Fact]
    public async Task ChangingPassword_InvalidatesOpenLinks()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("degisti");
        await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        await ForgotAsync(client, person);
        string token = TokenFrom(outbox.To(person).Single());
        string access = AccessToken(await LoginAsync(client, person));

        (await StatusAsync(client, HttpMethod.Post, "/api/account/change-password", access, new { currentPassword = Password, newPassword = NewPassword, newPasswordRepeat = NewPassword })).ShouldBe(HttpStatusCode.NoContent);

        (await ResetAsync(client, token, "Baska-Sifre-2026")).Raw.ShouldContain(Invalid);
    }
}
