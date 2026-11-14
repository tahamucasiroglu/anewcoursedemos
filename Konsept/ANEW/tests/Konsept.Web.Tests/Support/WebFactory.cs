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
    /// <summary>Testteki servis anahtarı (bilinen sır olarak kaydedilmeli; logda hiçbir biçimde geçmez).</summary>
    public const string ServiceKey = "web-test-service-key-0008";

    /// <summary>Gets sahte API istemcisi.</summary>
    public IKonseptApiClient Api { get; } = Substitute.For<IKonseptApiClient>();

    /// <summary>Gets bu örneğin log klasörü.</summary>
    public string LogDirectory { get; } = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "web-logs-" + Guid.NewGuid().ToString("N"));

    /// <summary>Log klasöründeki tüm log metnini okur (dosya Serilog tarafından açıkken de).</summary>
    /// <returns>Metin.</returns>
    public string ReadLogText() =>
        !Directory.Exists(LogDirectory)
            ? string.Empty
            : string.Concat(Directory.GetFiles(LogDirectory, "*.log").Select(f =>
            {
                using var stream = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }));

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
        builder.UseSetting("Logging:Directory", LogDirectory);
        builder.UseSetting("Internal:ServiceKey", ServiceKey);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IKonseptApiClient>();
            services.AddSingleton(Api);
        });
    }
}
