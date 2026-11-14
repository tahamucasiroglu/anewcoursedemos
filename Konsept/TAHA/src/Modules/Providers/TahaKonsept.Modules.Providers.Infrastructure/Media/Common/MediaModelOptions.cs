namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Common;

/// <summary>
/// Görsel/ses modellerinin zamanlama ayarları: asenkron işlerde (BFL, Replicate) durum sorgulama aralığı ve en uzun bekleme.
/// Testler sıfır aralıkla kurar; üretimde <see cref="Default"/>.
/// </summary>
/// <param name="PollInterval">Durum sorguları arası bekleme.</param>
/// <param name="MaxPollDuration">İşin tamamlanması için en uzun bekleme.</param>
public sealed record MediaModelOptions(TimeSpan PollInterval, TimeSpan MaxPollDuration)
{
    /// <summary>Üretim varsayılanı: 1.5 sn aralık, 5 dk üst sınır.</summary>
    public static MediaModelOptions Default { get; } = new(TimeSpan.FromMilliseconds(1500), TimeSpan.FromMinutes(5));
}
