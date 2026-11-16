using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Konsept.Modules.Identity.Presentation.Controllers;

/// <summary>
/// Ajan cihaz eşleştirme uçları (spec 0012). Kimliksiz olanlar yalnızca ajanın kod alması ve kod durumunu öğrenmesidir
/// (Req 10); sorgu, onay ve ret giriş ister. Kod alma, sorgu, onay ve ret IP başına hız sınırındadır (Req 8); durum sorgusu
/// tahmin edilemez onay sırrıyla çalıştığı için sınırsızdır (plan Risk 7).
/// </summary>
[ApiController]
[Route("api/agent-pairing")]
public sealed class AgentPairingController : ControllerBase
{
    private readonly IAgentPairingService _pairing;

    /// <summary>Initializes a new instance of the <see cref="AgentPairingController"/> class.</summary>
    /// <param name="pairing">Eşleştirme servisi.</param>
    public AgentPairingController(IAgentPairingService pairing)
    {
        _pairing = pairing;
    }

    /// <summary>Eşleştirilmemiş ajana kod verir.</summary>
    /// <param name="request">Makine adı ve ajan sürümü.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 kod + sır + son geçerlilik · 400 alan hatası · 429 çok fazla deneme.</returns>
    [AllowAnonymous]
    [EnableRateLimiting(AuthPolicies.AgentPairingRateLimit)]
    [HttpPost("codes")]
    public Task<IActionResult> CreateCode([FromBody] CreatePairingCodeRequest request, CancellationToken cancellationToken) =>
        _pairing.CreateCodeAsync(request, cancellationToken).ToActionResult();

    /// <summary>Ajanın kod durumunu döndürür; onaylandıysa ajan oturum belirtecini bir kez verir.</summary>
    /// <param name="request">Onay sırrı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 durum.</returns>
    [AllowAnonymous]
    [HttpPost("status")]
    public Task<IActionResult> Status([FromBody] PairingStatusRequest request, CancellationToken cancellationToken) =>
        _pairing.GetStatusAsync(request, cancellationToken).ToActionResult();

    /// <summary>Kodun cihazını gösterir.</summary>
    /// <param name="code">Kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 cihaz · 404 kod yok · 409 süresi dolmuş/kullanılmış · 429.</returns>
    [EnableRateLimiting(AuthPolicies.AgentPairingRateLimit)]
    [HttpGet("{code}")]
    public Task<IActionResult> Lookup(string code, CancellationToken cancellationToken) =>
        _pairing.LookupAsync(code, cancellationToken).ToActionResult();

    /// <summary>Kodu onaylar; ajan isteği yapan kullanıcının olur.</summary>
    /// <param name="code">Kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 cihaz · 404 · 409 · 429.</returns>
    [EnableRateLimiting(AuthPolicies.AgentPairingRateLimit)]
    [HttpPost("{code}/approve")]
    public Task<IActionResult> Approve(string code, CancellationToken cancellationToken) =>
        _pairing.ApproveAsync(code, cancellationToken).ToActionResult();

    /// <summary>Kodu reddeder.</summary>
    /// <param name="code">Kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 cihaz · 404 · 409 · 429.</returns>
    [EnableRateLimiting(AuthPolicies.AgentPairingRateLimit)]
    [HttpPost("{code}/reject")]
    public Task<IActionResult> Reject(string code, CancellationToken cancellationToken) =>
        _pairing.RejectAsync(code, cancellationToken).ToActionResult();
}
