namespace TahaKonsept.Modules.Diagnostics.Application.Abstract;

/// <summary>
/// Yönetim paneline istatistik veren modül sözleşmesi. Her modül bir uygulama yazıp kaydeder:
/// <c>services.AddScoped&lt;IStatsContributor, ConceptsStatsContributor&gt;();</c> (scoped; kendi DbContext'ini kullanabilir).
/// Diagnostics hepsini toplar (<c>GET api/diagnostics/admin/stats</c>); hata veren katkıcı atlanır, diğerleri gösterilir.
/// Tanınan anahtarlar ve değer biçimleri <see cref="Common.StatsKeys"/> içinde (ör. <c>videos.total</c> → int,
/// <c>videos.perDay</c> → <c>IReadOnlyDictionary&lt;DateOnly, int&gt;</c>). Tanınmayan sayısal/metin değerler "Extra" altında gösterilir.
/// </summary>
public interface IStatsContributor
{
    /// <summary>Bu modülün istatistiklerini üretir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Anahtar → değer.</returns>
    Task<IReadOnlyDictionary<string, object>> GetStatsAsync(CancellationToken cancellationToken);
}
