using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>Hatalı giriş kilidi ve IP hız sınırı (spec 0004 AC-6).</summary>
[Trait("Category", "Integration")]
public sealed class LoginLockoutTests
{
    private const string Locked = "Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin.";
    private const string Wrong = "Yanlis-Sifre-9";
    private readonly TestDatabase database;

    public LoginLockoutTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Login_ThreeFailures_LocksEvenCorrectPasswordFor15Minutes()
    {
        FakeTimeProvider time = new(DateTimeOffset.UtcNow);
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);

        (await StatusAsync(client, school.FounderEmail, Wrong)).ShouldBe(HttpStatusCode.Unauthorized);
        (await StatusAsync(client, school.FounderEmail, Wrong)).ShouldBe(HttpStatusCode.Unauthorized);
        (await DetailAsync(client, school.FounderEmail, Wrong)).ShouldBe(Locked);

        (await DetailAsync(client, school.FounderEmail, Password)).ShouldBe(Locked);
        time.Advance(TimeSpan.FromMinutes(14));
        (await DetailAsync(client, school.FounderEmail, Password)).ShouldBe(Locked);
        time.Advance(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1));
        (await StatusAsync(client, school.FounderEmail, Password)).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_TwoFailuresThenSuccess_ResetsCounter()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);

        await StatusAsync(client, school.FounderEmail, Wrong);
        await StatusAsync(client, school.FounderEmail, Wrong);
        (await StatusAsync(client, school.FounderEmail, Password)).ShouldBe(HttpStatusCode.OK);
        await StatusAsync(client, school.FounderEmail, Wrong);
        (await DetailAsync(client, school.FounderEmail, Wrong)).ShouldBe("E-posta veya şifre hatalı");

        (await StatusAsync(client, school.FounderEmail, Password)).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_UnknownEmail_GetsSameLockMessageAfterThreeAttempts()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        string unknown = UniqueEmail("yok");

        await StatusAsync(client, unknown, Wrong);
        await StatusAsync(client, unknown, Wrong);

        (await DetailAsync(client, unknown, Wrong)).ShouldBe(Locked);
        (await DetailAsync(client, unknown, Wrong)).ShouldBe(Locked);
    }

    [Fact]
    public async Task Login_ParallelFailures_StillLockAccount()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);

        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => StatusAsync(client, school.FounderEmail, Wrong)));

        (await DetailAsync(client, school.FounderEmail, Password)).ShouldBe(Locked);
    }

    [Fact]
    public async Task Login_MoreThan20PerMinuteFromSameIp_Returns429()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString, loginRateLimitPerMinute: 20);
        using HttpClient client = factory.CreateClient();

        for (int i = 0; i < 20; i++)
        {
            (await StatusAsync(client, UniqueEmail("hiz"), Wrong)).ShouldBe(HttpStatusCode.Unauthorized);
        }

        using HttpResponseMessage limited = await PostLoginAsync(client, UniqueEmail("hiz"), Wrong);
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await ReadJsonAsync(limited)).GetProperty("detail").GetString().ShouldBe("Çok fazla istek, biraz sonra deneyin.");
    }

    private static async Task<HttpStatusCode> StatusAsync(HttpClient client, string email, string password)
    {
        using HttpResponseMessage response = await PostLoginAsync(client, email, password);
        return response.StatusCode;
    }

    private static async Task<string?> DetailAsync(HttpClient client, string email, string password)
    {
        using HttpResponseMessage response = await PostLoginAsync(client, email, password);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        JsonElement body = await ReadJsonAsync(response);
        return body.GetProperty("detail").GetString();
    }
}
