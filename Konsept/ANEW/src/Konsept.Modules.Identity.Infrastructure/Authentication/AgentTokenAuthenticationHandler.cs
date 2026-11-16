using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Shared.Infrastructure.Http;
using Konsept.Shared.Infrastructure.Http.Base;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Konsept.Modules.Identity.Infrastructure.Authentication;

/// <summary>
/// Ajan hub'ının kimlik şeması (spec 0012 Req 10, <see cref="AuthPolicies.AgentScheme"/>): <c>Authorization: Bearer</c>
/// başlığındaki ajan oturum belirteci <see cref="IAgentTokenValidator"/> ile doğrulanır; geçerliyse principal <c>sub</c>
/// (sahip) ve <see cref="AuthPolicies.AgentDeviceClaim"/> (cihaz) taşır. Belirteç adreste taşınmaz ve hiçbir yere yazılmaz.
/// Çerçeve adaptörü; karar doğrulayıcıdadır.
/// </summary>
/// <param name="options">Şema seçenekleri.</param>
/// <param name="logger">Log fabrikası.</param>
/// <param name="encoder">URL kodlayıcı.</param>
/// <param name="validator">Belirteç doğrulayıcı.</param>
public sealed class AgentTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAgentTokenValidator validator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string BearerPrefix = "Bearer ";

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var principal = await validator.ValidateAsync(header[BearerPrefix.Length..].Trim(), Context.RequestAborted).ConfigureAwait(false);
        if (principal is null)
        {
            return AuthenticateResult.Fail("Ajan oturumu geçersiz.");
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(CurrentUserBase.SubjectClaim, principal.UserId.ToString("D", CultureInfo.InvariantCulture)),
                new Claim(AuthPolicies.AgentDeviceClaim, principal.DeviceId.ToString("D", CultureInfo.InvariantCulture)),
            ],
            Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
