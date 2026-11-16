using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>
/// Deneme çağrısı (plan 0013 Risk 3): isteği yapan kullanıcı adına sabit kısa bir istemle kapıyı çağırır (amaç "deneme"). Kapının
/// tüm kuralları aynen uygulanır; kayıt ve maliyet oluşur.
/// </summary>
public interface ITrialCallService
{
    /// <summary>Deneme çağrısını yapar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<TrialCallDto>> CallAsync(TrialCallRequest? request, CancellationToken cancellationToken);
}
