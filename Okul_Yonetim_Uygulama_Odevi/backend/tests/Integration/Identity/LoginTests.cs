using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>Giriş (spec 0004 AC-3, AC-5): tek okullu kişi okul seçilmiş olarak girer; hatalı bilgi tek tip yanıt alır.</summary>
[Trait("Category", "Integration")]
public sealed class LoginTests
{
    private readonly TestDatabase database;

    public LoginTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Login_SingleSchool_SelectsSchoolAndMeShowsNameSchoolRolesBranches()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);

        JsonElement auth = await LoginAsync(client, school.FounderEmail);

        auth.GetProperty("requiresTenantSelection").GetBoolean().ShouldBeFalse();
        auth.GetProperty("tenants").GetArrayLength().ShouldBe(1);
        auth.GetProperty("refreshToken").GetString().ShouldNotBeNullOrEmpty();
        using HttpResponseMessage me = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", AccessToken(auth));
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement body = await ReadJsonAsync(me);
        body.GetProperty("fullName").GetString().ShouldBe("Ayşe Kurucu");
        body.GetProperty("tenant").GetProperty("name").GetString().ShouldBe(school.SchoolName);
        body.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["Founder"]);
        body.GetProperty("branches").EnumerateArray().Select(b => b.GetProperty("name").GetString()).ShouldBe(["Merkez"]);
    }

    [Fact]
    public async Task Login_EmailWithCapitalsAndSpaces_MatchesSameAccount()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);

        using HttpResponseMessage response = await PostLoginAsync(client, "  " + school.FounderEmail.ToUpperInvariant() + " ", Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WebDelivery_SetsHttpOnlyStrictCookieAndNoRefreshTokenInBody()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);

        using HttpResponseMessage response = await PostLoginAsync(client, school.FounderEmail, Password, refreshTokenInBody: false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement body = await ReadJsonAsync(response);
        body.GetProperty("refreshToken").ValueKind.ShouldBe(JsonValueKind.Null);
        string cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("okul_refresh=", StringComparison.Ordinal));
        cookie.ShouldContain("httponly", Case.Insensitive);
        cookie.ShouldContain("samesite=strict", Case.Insensitive);
        cookie.ShouldContain("path=/api/auth", Case.Insensitive);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_ReturnSameStatusAndBody()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);

        using HttpResponseMessage wrongPassword = await PostLoginAsync(client, school.FounderEmail, "Yanlis-Sifre-1");
        using HttpResponseMessage unknownEmail = await PostLoginAsync(client, UniqueEmail("yok"), "Yanlis-Sifre-1");

        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        JsonElement a = await ReadJsonAsync(wrongPassword);
        JsonElement b = await ReadJsonAsync(unknownEmail);
        a.GetProperty("detail").GetString().ShouldBe("E-posta veya şifre hatalı");
        b.GetProperty("detail").GetString().ShouldBe(a.GetProperty("detail").GetString());
        Names(a).ShouldBe(Names(b));
    }

    [Theory]
    [InlineData("", "Bir-sifre-1")]
    [InlineData("gecersiz-eposta", "Bir-sifre-1")]
    [InlineData("biri@ornek.test", "")]
    public async Task Login_InvalidFields_Returns400ValidationError(string email, string password)
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await PostLoginAsync(client, email, password);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static string[] Names(JsonElement body) => [.. body.EnumerateObject().Select(p => p.Name).Where(n => n != "traceId" && n != "instance").Order()];
}
