namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// PORT (Concepts → üretim tarafı): kullanıcının video üretiminde seçili ses modeline göre anlatım hızı (kelime/sn).
/// Konsept anlatımının uzunluğu hedef süreye bu hızla boyutlandırılır. Varsayılan uygulama (<c>DefaultNarrationPaceSource</c>)
/// bilgi vermez (<c>null</c> → dil sabiti); API host'unda Production modülünün adaptörü kaydedilir (seçili model + ölçülmüş hız).
/// Önemli metot: <see cref="GetWordsPerSecondAsync"/>.
/// </summary>
public interface INarrationPaceSource
{
    /// <summary>Kullanıcının seçili ses modeline göre anlatım hızı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="language">Video dili (ör. <c>tr</c>).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kelime/sn; bilinmiyorsa (ses kapalı, model yok) <c>null</c>.</returns>
    Task<double?> GetWordsPerSecondAsync(Guid userId, string language, CancellationToken cancellationToken = default);
}
