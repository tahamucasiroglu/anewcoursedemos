using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Ajan cihaz eşleştirme (spec 0012 Req 1–8): ajan kod alır ve durumunu sorar (giriş istemez); kullanıcı web'den kodu
/// sorgular, onaylar ya da reddeder (giriş ister). Kodu kim onaylarsa ajan onundur; yeni eşleştirme öncekini iptal eder.
/// </summary>
public interface IAgentPairingService
{
    /// <summary>Eşleştirilmemiş ajana yeni kod ve onay sırrı verir.</summary>
    /// <param name="request">Ajanın makine adı ve sürümü.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kod, sır ve son geçerlilik.</returns>
    Task<Result<PairingCodeDto>> CreateCodeAsync(CreatePairingCodeRequest request, CancellationToken cancellationToken);

    /// <summary>Ajanın kod durumunu döndürür; onaylandıysa ajan oturum belirtecini bir kez verir.</summary>
    /// <param name="request">Onay sırrı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    Task<Result<PairingStatusDto>> GetStatusAsync(PairingStatusRequest request, CancellationToken cancellationToken);

    /// <summary>Kodun ait olduğu cihazı gösterir (onaydan önce).</summary>
    /// <param name="code">Kullanıcının yazdığı kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz ya da hata (yok / süresi dolmuş / kullanılmış).</returns>
    Task<Result<PairingDeviceDto>> LookupAsync(string? code, CancellationToken cancellationToken);

    /// <summary>Kodu onaylar: ajan isteği yapan kullanıcının olur.</summary>
    /// <param name="code">Kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz ya da hata.</returns>
    Task<Result<PairingDeviceDto>> ApproveAsync(string? code, CancellationToken cancellationToken);

    /// <summary>Kodu reddeder.</summary>
    /// <param name="code">Kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz ya da hata.</returns>
    Task<Result<PairingDeviceDto>> RejectAsync(string? code, CancellationToken cancellationToken);
}
