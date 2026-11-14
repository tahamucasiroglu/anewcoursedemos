using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Ses modeline göre konuşma hızı (kelime/sn): <see cref="GetWordsPerSecondAsync"/> etkin hızı verir — önce bu kullanıcının
/// o model + ses için ölçülmüş ortalaması, yoksa katalogdaki model hızı (<c>ModelInfo.WordsPerSecond</c>, Türkçe), yoksa yerel/online
/// varsayılanı; sonuç ayarlardaki hız çarpanıyla (<c>SpeechSpeed</c>) çarpılır. <see cref="RecordAsync"/> üretilen videodan ölçüm ekler
/// (kendini ayarlama). Konsept üretimi (Concepts portu) ve ScenePlan süre uyarısı bunu kullanır.
/// </summary>
public interface ISpeechRateService
{
    /// <summary>Verilen ayarlarla etkin hız.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="settings">Üretim ayarları.</param>
    /// <param name="language">Dil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kelime/sn; ses kapalıysa ya da dil için bilgi yoksa <c>null</c>.</returns>
    Task<double?> GetWordsPerSecondAsync(Guid userId, ProductionSettingsDto settings, string language, CancellationToken cancellationToken);

    /// <summary>Kullanıcının kayıtlı üretim ayarlarıyla etkin hız.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="language">Dil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kelime/sn ya da <c>null</c>.</returns>
    Task<double?> GetForUserAsync(Guid userId, string language, CancellationToken cancellationToken);

    /// <summary>Üretilen videonun ölçümünü kaydeder (ses kapalı / yetersiz / makul olmayan veri atlanır).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="settings">Videonun üretildiği ayarlar.</param>
    /// <param name="language">Dil.</param>
    /// <param name="words">Yazılış kelime sayısı.</param>
    /// <param name="seconds">Gerçek seslendirme süresi (sn).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıttan sonraki ortalama (1.0× hıza indirgenmiş) ya da atlandıysa <c>null</c>.</returns>
    Task<double?> RecordAsync(Guid userId, ProductionSettingsDto settings, string language, int words, double seconds, CancellationToken cancellationToken);
}
