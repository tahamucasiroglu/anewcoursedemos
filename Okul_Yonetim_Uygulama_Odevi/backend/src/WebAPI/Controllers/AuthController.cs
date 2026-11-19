using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity;
using OkulYonetim.Application.Features.Identity.Commands.Login;
using OkulYonetim.Application.Features.Identity.Commands.Logout;
using OkulYonetim.Application.Features.Identity.Commands.RefreshSession;
using OkulYonetim.Application.Features.Identity.Commands.SelectTenant;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Giriş ve oturum uçları (spec 0004). Anonim olan yalnızca giriş ve yenileme (açıkça listelenen istisnalar); giriş IP
/// başına hız sınırlıdır. Web yenileme anahtarını HttpOnly çerezle, mobil yanıt gövdesiyle alır. Hatalar ProblemDetails:
/// hatalı giriş/kilit/geçersiz oturum 401, doğrulama 400, hız sınırı 429, başka okul 404.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender sender;

    public AuthController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimit.PolicyName)]
    [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthTokensResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AuthResult result = await this.sender.Send(new LoginCommand(request.Email ?? string.Empty, request.Password ?? string.Empty), cancellationToken).ConfigureAwait(false);
        return this.Deliver(result, request.RefreshTokenInBody);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokensResponse>> Refresh(RefreshRequest? request, CancellationToken cancellationToken)
    {
        bool inBody = !string.IsNullOrEmpty(request?.RefreshToken);
        string token = inBody ? request!.RefreshToken! : RefreshTokenCookie.Read(this.Request) ?? string.Empty;
        AuthResult result = await this.sender.Send(new RefreshSessionCommand(token), cancellationToken).ConfigureAwait(false);
        return this.Deliver(result, inBody);
    }

    [HttpPost("select-tenant")]
    [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthTokensResponse>> SelectTenant(SelectTenantRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AuthResult result = await this.sender.Send(new SelectTenantCommand(request.TenantId), cancellationToken).ConfigureAwait(false);
        return this.Ok(AuthTokensResponse.From(result, includeRefreshToken: false));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await this.sender.Send(new LogoutCommand(), cancellationToken).ConfigureAwait(false);
        RefreshTokenCookie.Delete(this.Response);
        return this.NoContent();
    }

    private OkObjectResult Deliver(AuthResult result, bool refreshTokenInBody)
    {
        if (!refreshTokenInBody && result.RefreshToken is { } refreshToken)
        {
            RefreshTokenCookie.Write(this.Response, refreshToken);
        }

        return this.Ok(AuthTokensResponse.From(result, includeRefreshToken: refreshTokenInBody));
    }
}
