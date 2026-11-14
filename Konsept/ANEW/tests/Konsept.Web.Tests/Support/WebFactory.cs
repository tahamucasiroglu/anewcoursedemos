using AngleSharp;
using AngleSharp.Dom;
using Konsept.Web.ApiClient.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Konsept.Web.Tests.Support;

/// <summary>
/// Web'i süreç içinde açar; API istemcisi sahtedir (Web testleri API'ye gitmez).
/// </summary>
public sealed class WebFactory : WebApplicationFactory<Program>
{
    /// <summary>Gets sahte API istemcisi.</summary>
    public IKonseptApiClient Api { get; } = Substitute.For<IKonseptApiClient>();

    /// <summary>Sayfayı getirir ve AngleSharp belgesine çevirir.</summary>
    /// <param name="client">HTTP istemcisi.</param>
    /// <param name="path">Sayfa yolu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>HTML belgesi.</returns>
    public static async Task<IDocument> GetDocumentAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        var html = await client.GetStringAsync(new Uri(path, UriKind.Relative), cancellationToken).ConfigureAwait(false);
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(request => request.Content(html), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("Api:BaseUrl", "http://api.invalid/");
        builder.UseSetting("Logging:Directory", Path.Combine(Path.GetTempPath(), "anewconcept-tests", "logs"));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IKonseptApiClient>();
            services.AddSingleton(Api);
        });
    }
}
