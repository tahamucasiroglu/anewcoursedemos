using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Konsept.Shared.Kernel.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Konsept.Shared.Infrastructure.Http;

/// <summary>
/// Web ↔ Api iç uçlarının kimlik şeması (plan 0002, Risk 11): <see cref="ServiceKeyHeader.Name"/> başlığı yapılandırılmış
/// servis anahtarıyla sabit zamanlı karşılaştırmada eşleşirse istek "Web servisi" kimliğiyle doğrulanır; aksi halde
/// sonuç yoktur (iç uç politikası 401 döner). Kullanıcı kimliği vermez. Çerçeve adaptörü.
/// </summary>
/// <param name="options">Şema seçenekleri.</param>
/// <param name="logger">Log fabrikası.</param>
/// <param name="encoder">URL kodlayıcı.</param>
/// <param name="configuration">Yapılandırma (<see cref="ServiceKeyHeader.ConfigurationKey"/>).</param>
public sealed class ServiceKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>İstekteki servis anahtarı yapılandırılmış anahtarla eşleşiyor mu (anahtar yoksa her zaman hayır).</summary>
    /// <param name="configuration">Yapılandırma.</param>
    /// <param name="request">HTTP isteği.</param>
    /// <returns>Eşleşiyorsa <c>true</c>.</returns>
    public static bool HasValidServiceKey(IConfiguration configuration, HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(request);
        var expected = configuration[ServiceKeyHeader.ConfigurationKey];
        var given = request.Headers[ServiceKeyHeader.Name].ToString();
        return !string.IsNullOrEmpty(expected) && !string.IsNullOrEmpty(given)
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(given));
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!HasValidServiceKey(configuration, Request))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "konsept-web")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
