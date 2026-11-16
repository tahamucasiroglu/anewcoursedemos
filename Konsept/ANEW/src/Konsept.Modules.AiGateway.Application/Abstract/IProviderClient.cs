using Konsept.Modules.AiGateway.Application.Dtos;

namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>
/// Sağlayıcıya tek bir deneme gönderir (uygulaması Infrastructure'da ham HTTP). Kendisi asla tekrar etmez ve zaman aşımı
/// uygulamaz (ikisi de kapının kararıdır); iptal edilirse <see cref="OperationCanceledException"/> fırlatır. Sağlayıcının ham
/// yanıtı dışarı verilmez.
/// </summary>
public interface IProviderClient
{
    /// <summary>Tek denemeyi gönderir.</summary>
    /// <param name="call">Çağrı.</param>
    /// <param name="cancellationToken">İptal (zaman aşımı dahil).</param>
    /// <returns>Deneme sonucu.</returns>
    Task<ProviderAttempt> SendAsync(ProviderCall call, CancellationToken cancellationToken);
}
