using Konsept.Modules.Identity.Application.Dtos;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Ajan oturum belirtecini doğrular (spec 0012 Req 9–10): iptal edilmemiş ve 90 gün içinde kullanılmış cihazın belirteci
/// geçerlidir; geçerliyse son kullanım (en fazla dakikada bir) kaydırılır.
/// </summary>
public interface IAgentTokenValidator
{
    /// <summary>Belirteci doğrular.</summary>
    /// <param name="token">Ajanın sunduğu belirteç.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sahip ve cihaz; geçersizse <c>null</c>.</returns>
    Task<AgentPrincipal?> ValidateAsync(string? token, CancellationToken cancellationToken);
}
