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

/// <summary>Spec 0006 AC-1, AC-2: kendi profilim — ad/telefon değişir, e-posta değişmez, alan hataları; yalnızca kendim; okul seçmemiş kişi de.</summary>
[Trait("Category", "Integration")]
public sealed class ProfileTests
{
    private readonly TestDatabase database;

    public ProfileTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task UpdateOwnProfile_ShowsEverywhere_EmailUnchanged_InvalidFieldsRejected()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string person = UniqueEmail("profil");
        Guid personId = await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        await AddMemberAsync(factory, person, b.TenantId, Role.Guardian, b.BranchId);
        string token = await TokenForSchoolAsync(client, person, a.TenantId);

        var (status, body, _) = await CallAsync(client, HttpMethod.Put, "/api/account/profile", token, new { fullName = "  Ayşe Yılmaz ", phone = "0532 111 22 33", email = "baska@ornek.test" });

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("fullName").GetString().ShouldBe("Ayşe Yılmaz");
        body.GetProperty("phone").GetString().ShouldBe("+905321112233");
        body.GetProperty("email").GetString().ShouldBe(person);
        var (_, me, _) = await CallAsync(client, HttpMethod.Get, "/api/me", token);
        me.GetRawText().ShouldContain("Ayşe Yılmaz");
        string founderB = await TokenForSchoolAsync(client, b.FounderEmail, b.TenantId);
        var (_, card, _) = await CallAsync(client, HttpMethod.Get, $"/api/admin/members/{personId}", founderB);
        card.GetProperty("fullName").GetString().ShouldBe("Ayşe Yılmaz");
        card.GetProperty("phone").GetString().ShouldBe("+905321112233");

        foreach (object invalid in new object[] { new { fullName = "", phone = (string?)null }, new { fullName = new string('a', 101), phone = (string?)null }, new { fullName = "Ad", phone = "12345" } })
        {
            (await StatusAsync(client, HttpMethod.Put, "/api/account/profile", token, invalid)).ShouldBe(HttpStatusCode.BadRequest);
        }

        (await QueryAsync(factory, c => c.Users.Where(u => u.Id == personId).Select(u => u.FullName).SingleAsync())).ShouldBe("Ayşe Yılmaz");
    }

    [Fact]
    public async Task Anonymous_IsRejected_AndPersonWithoutSelectedSchoolCanUseAccount()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        SchoolSeed b = await CreateSchoolAsync(factory);
        string person = UniqueEmail("iki-okul");
        await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        await AddMemberAsync(factory, person, b.TenantId, Role.Teacher, b.BranchId);

        using (HttpResponseMessage anonymous = await client.GetAsync(new Uri("/api/account/profile", UriKind.Relative), TestContext.Current.CancellationToken))
        {
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        JsonElement auth = await LoginAsync(client, person);
        auth.GetProperty("requiresTenantSelection").GetBoolean().ShouldBeTrue();
        (await StatusAsync(client, HttpMethod.Get, "/api/account/profile", AccessToken(auth))).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Put, "/api/account/profile", AccessToken(auth), new { fullName = "Seçmeden Değişti", phone = (string?)null })).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Post, "/api/account/change-password", AccessToken(auth), new { currentPassword = Password, newPassword = NewPassword, newPasswordRepeat = NewPassword })).ShouldBe(HttpStatusCode.NoContent);
        (await QueryAsync(factory, c => c.Users.CountAsync(u => u.FullName == "Seçmeden Değişti" && u.Email == person))).ShouldBe(1);
    }
}
