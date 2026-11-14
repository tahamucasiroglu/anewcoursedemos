using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Http.Base;

namespace TahaKonsept.Agent.Core.Http.Base;

/// <summary>
/// Ajan → API istemci tabanı. Taban adres sabit değildir: her istekte göreli adres güncel
/// <c>AgentSettings.ServerBaseUrl</c>'e göre mutlak yapılır (ayarlardan sunucu değişince yeniden başlatma gerekmez).
/// </summary>
public abstract class AgentApiClientBase : ApiHttpClientBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="httpClient">HttpClient (taban adressiz).</param>
    /// <param name="settings">Ayar deposu.</param>
    protected AgentApiClientBase(HttpClient httpClient, IAgentSettingsStore settings)
        : base(httpClient)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>Ayar deposu.</summary>
    protected IAgentSettingsStore Settings { get; }

    /// <summary>Göreli ya da mutlak adresi güncel sunucuya göre mutlak yapar.</summary>
    /// <param name="uri">Adres.</param>
    /// <returns>Mutlak adres.</returns>
    public Uri ToAbsolute(string uri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        return Uri.TryCreate(uri, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https"
            ? absolute
            : new Uri(BaseUri(), uri.TrimStart('/'));
    }

    /// <inheritdoc />
    protected override Task PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { IsAbsoluteUri: false } relative)
        {
            request.RequestUri = new Uri(BaseUri(), relative.OriginalString.TrimStart('/'));
        }

        return Task.CompletedTask;
    }

    private Uri BaseUri()
    {
        var baseUrl = Settings.Current.ServerBaseUrl.TrimEnd('/') + "/";
        return new Uri(baseUrl, UriKind.Absolute);
    }
}
