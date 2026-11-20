using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Account.AccountTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Account;

/// <summary>Spec 0006 AC-5, AC-6: şifremi unuttum isteği — her durumda aynı yanıt; kayıtlıya tek e-posta; e-posta başına ve IP başına sınır.</summary>
[Trait("Category", "Integration")]
public sealed class ForgotPasswordTests
{
    private readonly TestDatabase database;

    public ForgotPasswordTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task KnownAndUnknownEmail_GetIdenticalResponse_OnlyKnownGetsOneEmail()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("unuttum");
        await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        string unknown = UniqueEmail("yok");

        var known = await ForgotAsync(client, "  " + person.ToUpperInvariant() + " ");
        var missing = await ForgotAsync(client, unknown);

        known.Status.ShouldBe(HttpStatusCode.OK);
        missing.Status.ShouldBe(HttpStatusCode.OK);
        known.Raw.ShouldBe(missing.Raw);
        JsonDocument.Parse(known.Raw).RootElement.GetProperty("message").GetString().ShouldBe(ResetMessage);
        EmailMessage[] sent = outbox.To(person);
        sent.Length.ShouldBe(1);
        sent[0].Subject.ShouldBe("Okul Yönetim — şifre yenileme");
        sent[0].Body.ShouldContain("Test Kişi");
        sent[0].Body.ShouldContain("30 dakika geçerlidir");
        sent[0].Body.ShouldContain("Bu isteği siz yapmadıysanız bu e-postayı yok sayın; şifreniz değişmez.");
        TokenFrom(sent[0]).Length.ShouldBeGreaterThan(30);
        outbox.To(unknown).ShouldBeEmpty();

        (await ForgotAsync(client, "")).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await ForgotAsync(client, "gecersiz")).Status.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FourthRequestWithin15Minutes_SameMessageButNoEmail_ThenAllowedAgain()
    {
        CapturingOutbox outbox = new();
        FakeTimeProvider time = new(DateTimeOffset.UtcNow);
        using OkulApiFactory factory = Factory(this.database, outbox, time);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("sinir");
        await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);

        for (int i = 0; i < 4; i++)
        {
            var result = await ForgotAsync(client, person);
            result.Status.ShouldBe(HttpStatusCode.OK);
            JsonDocument.Parse(result.Raw).RootElement.GetProperty("message").GetString().ShouldBe(ResetMessage);
        }

        outbox.To(person).Length.ShouldBe(3);
        time.Advance(TimeSpan.FromMinutes(16));
        await ForgotAsync(client, person);
        outbox.To(person).Length.ShouldBe(4);
    }

    [Fact]
    public async Task PerIpLimit_Returns429()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox(), rateLimit: 2);
        using HttpClient client = factory.CreateClient();

        (await ForgotAsync(client, UniqueEmail())).Status.ShouldBe(HttpStatusCode.OK);
        (await ForgotAsync(client, UniqueEmail())).Status.ShouldBe(HttpStatusCode.OK);
        var limited = await ForgotAsync(client, UniqueEmail());

        limited.Status.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Raw.ShouldContain("Çok fazla istek");
    }
}
