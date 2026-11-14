using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Modules.Agents.Api.Controllers.Base;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Shared.Contracts.Agent.Pairing;

namespace TahaKonsept.Modules.Agents.Api.Controllers;

/// <summary>
/// Cihaz eşleme (RFC 8628 benzeri): <c>POST api/agents/device/start</c> ve <c>POST api/agents/device/token</c> anonim (ajan),
/// <c>POST api/agents/device/approve</c> ve <c>POST api/agents/device/deny</c> oturumlu (web). Yoklama hataları 400 + <c>code</c>
/// (<c>DeviceTokenErrorCodes</c>).
/// </summary>
[Route("api/agents/device")]
public sealed class DevicePairingController : AgentsControllerBase
{
    private readonly IDevicePairingService _service;

    /// <summary>Oluşturur.</summary>
    /// <param name="service">Eşleme servisi.</param>
    public DevicePairingController(IDevicePairingService service)
    {
        _service = service;
    }

    /// <summary>Eşleme başlatır.</summary>
    /// <param name="request">Bilgisayar adı + sürüm.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>DeviceStartResponse</c>.</returns>
    [AllowAnonymous]
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] DeviceStartRequest request, CancellationToken ct) =>
        ToActionResult(await _service.StartAsync(request, ct));

    /// <summary>Token yoklar.</summary>
    /// <param name="request">Cihaz kodu.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>TokenResponse</c> ya da 400 (bekleniyor/yavaşla/süre doldu/reddedildi).</returns>
    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] DeviceTokenRequest request, CancellationToken ct) =>
        ToActionResult(await _service.PollTokenAsync(request, ct));

    /// <summary>Kodu onaylar.</summary>
    /// <param name="request">Kullanıcı kodu.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>DeviceApproveResponse</c>.</returns>
    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody] DeviceApproveRequest request, CancellationToken ct) =>
        ToActionResult(await _service.ApproveAsync(request, ct));

    /// <summary>Kodu reddeder.</summary>
    /// <param name="request">Kullanıcı kodu.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpPost("deny")]
    public async Task<IActionResult> Deny([FromBody] DeviceApproveRequest request, CancellationToken ct) =>
        ToActionResult(await _service.DenyAsync(request, ct));
}
