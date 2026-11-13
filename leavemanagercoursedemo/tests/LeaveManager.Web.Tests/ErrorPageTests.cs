using System.Net;

namespace LeaveManager.Web.Tests;

[Trait("Category", "Integration")]
public sealed class ErrorPageTests(LeaveManagerWebFactory factory, ProductionWebFactory productionFactory)
    : IClassFixture<LeaveManagerWebFactory>, IClassFixture<ProductionWebFactory>
{
    // AC-4 (Testing environment)
    [Fact]
    public Task UnhandledException_InTesting_ReturnsGenericTurkishPage() =>
        AssertGenericErrorPageAsync(factory);

    // AC-4 (Production environment)
    [Fact]
    public Task UnhandledException_InProduction_ReturnsGenericTurkishPage() =>
        AssertGenericErrorPageAsync(productionFactory);

    private static async Task AssertGenericErrorPageAsync(LeaveManagerWebFactory webFactory)
    {
        using var client = webFactory.CreateClient();

        using var response = await client.GetAsync(
            new Uri(ThrowingTestController.Path, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Beklenmeyen bir hata oluştu", body, StringComparison.Ordinal);
        Assert.DoesNotContain(ThrowingTestController.SecretMessage, body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(ThrowingTestController), body, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", body, StringComparison.Ordinal);
    }
}
