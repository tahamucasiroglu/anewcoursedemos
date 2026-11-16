using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>
/// Tek çağrı kapısı (spec 0013, H-3, Y-7): online modeller yalnızca buradan çağrılır. Yetki ve key yoksa, efor desteklenmiyorsa
/// sağlayıcıya hiç gitmeden reddeder; çıktı sınırını katalogla kırpar; yalnızca 429/5xx/bağlantı kurulamadığında en fazla 2 kez
/// tekrar eder; zaman aşımında tekrar etmez; sağlayıcıya ulaşan her denemeyi maliyetiyle kaydeder. Harcama hiçbir çağrıyı
/// engellemez (BR-12).
/// </summary>
public interface IAiCallGateway
{
    /// <summary>Çağrıyı yapar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç ya da Türkçe nedenli hata.</returns>
    Task<Result<AiCallResult>> CallAsync(AiCallRequest request, CancellationToken cancellationToken);
}
