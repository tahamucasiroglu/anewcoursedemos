using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TahaKonsept.Web.Tests.Support;

/// <summary>
/// Test kimlik doğrulaması: istekte <see cref="UserHeader"/> başlığı varsa o adla oturum açmış sayar, yoksa sonuç üretmez
/// (meydan okuma gerçek cookie şemasında kalır → giriş sayfasına yönlendirme test edilebilir).
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Şema adı.</summary>
    public const string SchemeName = "Test";

    /// <summary>Kullanıcı adını taşıyan istek başlığı.</summary>
    public const string UserHeader = "X-Test-User";

    /// <summary>Taban bağımlılıkları alır.</summary>
    /// <param name="options">Şema seçenekleri.</param>
    /// <param name="logger">Günlükçü fabrikası.</param>
    /// <param name="encoder">URL kodlayıcı.</param>
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = Request.Headers[UserHeader].ToString();
        if (string.IsNullOrEmpty(user))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, user), new Claim(ClaimTypes.NameIdentifier, user)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
