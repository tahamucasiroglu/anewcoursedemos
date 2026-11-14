using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TahaKonsept.Shared.Contracts.Identity;
using TahaKonsept.Web.Infrastructure.Api.Abstract;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;
using TahaKonsept.Web.Infrastructure.Auth.Base;

namespace TahaKonsept.Web.Infrastructure.Auth.Concrete;

/// <summary>
/// <c>POST api/identity/refresh</c> ile token yeniler. <see cref="IApiClient"/> yerine ham, adlandırılmış HttpClient
/// kullanır (yenileme isteği kendi kendini yenilemeye çalışmasın). 2xx → yeni <see cref="TokenResponse"/>, 400/401/403 → null.
/// </summary>
public sealed class ApiTokenRefresher : TokenRefresherBase
{
    /// <summary>Ham API HttpClient adı.</summary>
    public const string HttpClientName = "TahaKonsept.Api.Raw";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="store">Token deposu.</param>
    /// <param name="cache">Bellek önbelleği.</param>
    /// <param name="httpClientFactory">HttpClient fabrikası.</param>
    /// <param name="logger">Günlükleyici.</param>
    public ApiTokenRefresher(ITokenStore store, IMemoryCache cache, IHttpClientFactory httpClientFactory, ILogger<ApiTokenRefresher> logger)
        : base(store, cache, logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    protected override async Task<TokenResponse?> RequestRefreshAsync(string refreshToken, CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await http.PostAsJsonAsync("api/identity/refresh", new RefreshRequest { RefreshToken = refreshToken }, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            if ((int)response.StatusCode >= 500)
            {
                throw new HttpRequestException($"Token yenileme sunucu hatası: {(int)response.StatusCode}");
            }

            return null;
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, ct);
        return tokens is null || string.IsNullOrEmpty(tokens.AccessToken) ? null : tokens;
    }
}
