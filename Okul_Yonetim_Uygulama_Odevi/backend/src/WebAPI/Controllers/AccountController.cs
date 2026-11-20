using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Account;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Hesabım (spec 0006): kendi profilim ve şifremi değiştir. Giriş ister, okul seçimi istemez; yalnızca oturumdaki kişinin
/// hesabı (başka kişinin kimliği alınmaz). Hatalar: alan 400, yanlış mevcut şifre / kilit 400 (oturum sorunu değil).
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly ISender sender;

    public AccountController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet("profile")]
    [ProducesResponseType<MyProfile>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyProfile>> GetProfile(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetMyProfileQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPut("profile")]
    [ProducesResponseType<MyProfile>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MyProfile>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new UpdateMyProfileCommand(request.FullName ?? string.Empty, request.Phone), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.sender.Send(new ChangePasswordCommand(request.CurrentPassword ?? string.Empty, request.NewPassword ?? string.Empty, request.NewPasswordRepeat ?? string.Empty), cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }
}
