using System.Net;

namespace LeaveManager.Web.Tests;

[Trait("Category", "Integration")]
public sealed class NotFoundPageTests(LeaveManagerWebFactory factory) : IClassFixture<LeaveManagerWebFactory>
{
    // AC-5
    [Fact]
    public async Task Get_UnknownPath_ReturnsTurkishNotFoundPage()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/olmayan-sayfa", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var document = await Html.ParseAsync(response);
        Assert.Equal("Sayfa bulunamadı", Assert.Single(document.QuerySelectorAll("h1")).TextContent.Trim());
        Assert.Contains(document.QuerySelectorAll("a"), link => link.GetAttribute("href") == "/");
    }
}
