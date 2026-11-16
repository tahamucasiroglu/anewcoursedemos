using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Contracts.Abstract;

/// <summary>
/// Başka modüllerin online metin modeli çağırdığı tek yol (Y-7, spec 0014): çağrı 0013 kapısından geçer — yetki (BR-13), çağıranın
/// kendi key'i (BR-1), efor, katalog çıktı sınırı, yalnızca 429/5xx/bağlantıda en fazla 2 tekrar ve sağlayıcıya ulaşan her denemenin
/// rol + profille harcama kaydı (BR-12, H-3). Çağıran kendi tekrarını eklemez.
/// </summary>
public interface IModelCaller
{
    /// <summary>İsteği yapan kullanıcı ve sunucuda kurulmuş yetkileri; giriş yoksa <c>null</c>.</summary>
    /// <returns>Kimlik.</returns>
    ModelCallerIdentity? Current();

    /// <summary>Metin modelini çağırır.</summary>
    /// <param name="call">Çağrı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yanıt ya da Türkçe, sade nedenli hata.</returns>
    Task<Result<TextModelReply>> CallTextAsync(TextModelCall call, CancellationToken cancellationToken);
}
