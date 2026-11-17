namespace Konsept.Modules.Monitoring.Contracts.Abstract;

/// <summary>
/// Klip istatistiği kaynağı (spec 0010 Req 3–4): Production uygular. Klip = üretimi tamamlanan video; aynı videonun (konseptin)
/// yeni sürümleri ayrı klip sayılmaz.
/// </summary>
public interface IClipStatistics
{
    /// <summary>Her klibin ilk tamamlanma anını listeler (video başına bir kayıt).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İlk tamamlanma anları (UTC).</returns>
    Task<IReadOnlyList<DateTimeOffset>> ListFirstCompletionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Kullanıcı başına klip sayısı (spec 0024 Req 1; klip tanımı 0010 Req 3: konsept başına bir klip, yeni sürüm ayrı sayılmaz).
    /// Yalnızca canlı video satırlarından sayılır (silinmiş videolar — backlog).
    /// </summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcı → klip sayısı (klibi olmayan kullanıcı yok).</returns>
    Task<IReadOnlyDictionary<Guid, int>> CountByUserAsync(CancellationToken cancellationToken);
}
