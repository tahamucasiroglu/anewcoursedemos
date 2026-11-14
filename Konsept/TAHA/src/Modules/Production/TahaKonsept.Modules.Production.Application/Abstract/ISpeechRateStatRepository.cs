using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Ölçülmüş konuşma hızı deposu (<see cref="SpeechRateStat"/>): kullanıcı + ses modeli + ses + dil anahtarıyla tek kayıt.
/// </summary>
public interface ISpeechRateStatRepository : IRepository<SpeechRateStat>
{
    /// <summary>Kaydı bulur (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="speechModelKey">Ses modeli.</param>
    /// <param name="voiceKey">Ses (boş = varsayılan).</param>
    /// <param name="language">Dil (küçük harf).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt ya da <c>null</c>.</returns>
    Task<SpeechRateStat?> FindAsync(Guid userId, string speechModelKey, string voiceKey, string language, CancellationToken cancellationToken);
}
