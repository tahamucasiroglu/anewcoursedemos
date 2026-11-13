using System.Net;

namespace LeaveManager.Web.Tests;

[Trait("Category", "Integration")]
public sealed class HomePageTests(LeaveManagerWebFactory factory) : IClassFixture<LeaveManagerWebFactory>
{
    // AC-1
    [Fact]
    public async Task Get_Root_Anonymous_ReturnsTurkishHomePage()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await Html.ParseAsync(response);
        Assert.Equal("tr", document.DocumentElement.GetAttribute("lang"));
        Assert.Contains("İzin Yönetimi", document.Title, StringComparison.Ordinal);
        var heading = Assert.Single(document.QuerySelectorAll("h1"));
        Assert.Equal("İzin Yönetimi", heading.TextContent.Trim());
    }

    // AC-2
    [Fact]
    public async Task Get_Root_LinksOwnStylesheet_AndHasNoInlineScript()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var document = await Html.ParseAsync(response);

        var stylesheet = Assert.Single(document.QuerySelectorAll("link[rel=stylesheet]"));
        var href = stylesheet.GetAttribute("href");
        Assert.NotNull(href);
        Assert.StartsWith("/css/site.css", href, StringComparison.Ordinal);
        using var css = await client.GetAsync(new Uri(href, UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, css.StatusCode);

        Assert.All(document.QuerySelectorAll("script"), script => Assert.True(script.HasAttribute("src")));
        Assert.DoesNotContain(
            document.All.SelectMany(element => element.Attributes),
            attribute => attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase));
    }
}
