namespace TahaKonsept.BuildingBlocks.Application.Abstract;

/// <summary>
/// Veritabanı hazır olma durumu (salt okunur). API başlarken migration'lar arka planda, ulaşılamazsa yeniden denenerek uygulanır;
/// veritabanına ihtiyaç duyan arka plan servisleri (ör. üretim orkestratörü) işe başlamadan önce <see cref="WaitUntilReadyAsync"/> çağırır.
/// Önemli üyeler: <see cref="IsReady"/>, <see cref="LastError"/>, <see cref="AttemptCount"/>, <see cref="WaitUntilReadyAsync"/>.
/// </summary>
public interface IDatabaseStatus
{
    /// <summary>Migration'lar başarıyla uygulandı ve veritabanı kullanılabilir mi.</summary>
    bool IsReady { get; }

    /// <summary>Son başarısız denemenin hata mesajı (hazırsa <c>null</c>).</summary>
    string? LastError { get; }

    /// <summary>Son deneme zamanı (UTC; henüz denenmediyse <c>null</c>).</summary>
    DateTimeOffset? LastAttemptUtc { get; }

    /// <summary>Şu ana kadarki migration deneme sayısı.</summary>
    int AttemptCount { get; }

    /// <summary>Veritabanı hazır olana kadar bekler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Hazır olunca tamamlanan görev.</returns>
    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);
}
