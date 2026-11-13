namespace LeaveManager.Web.Tests;

[Trait("Category", "Integration")]
public sealed class SecurityHeadersTests(LeaveManagerWebFactory factory) : IClassFixture<LeaveManagerWebFactory>
{
    // AC-3: home page, not-found page, error page and the stylesheet.
    [Theory]
    [InlineData("/")]
    [InlineData("/olmayan-sayfa")]
    [InlineData(ThrowingTestController.Path)]
    [InlineData("/css/site.css")]
    public async Task Response_HasSecurityHeaders(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("'unsafe-inline'", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("'unsafe-eval'", csp, StringComparison.Ordinal);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
    }
}
