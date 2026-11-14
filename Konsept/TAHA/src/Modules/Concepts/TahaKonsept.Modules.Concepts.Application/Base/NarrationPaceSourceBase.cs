using TahaKonsept.Modules.Concepts.Application.Abstract;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="INarrationPaceSource"/> ortak davranışı: somut sınıfın verdiği hızı (<see cref="ResolveAsync"/>) makul aralığa
/// (<see cref="MinWordsPerSecond"/>–<see cref="MaxWordsPerSecond"/>) sıkıştırır; aralık dışı / sayı olmayan değer <c>null</c> olur.
/// </summary>
public abstract class NarrationPaceSourceBase : INarrationPaceSource
{
    /// <summary>Kabul edilen en düşük hız (kelime/sn).</summary>
    public const double MinWordsPerSecond = 0.8;

    /// <summary>Kabul edilen en yüksek hız (kelime/sn).</summary>
    public const double MaxWordsPerSecond = 4.5;

    /// <summary>Hızı verir (sıkıştırılmış).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="language">Dil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kelime/sn ya da <c>null</c>.</returns>
    public async Task<double?> GetWordsPerSecondAsync(Guid userId, string language, CancellationToken cancellationToken = default)
    {
        var value = await ResolveAsync(userId, string.IsNullOrWhiteSpace(language) ? "tr" : language.Trim().ToLowerInvariant(), cancellationToken);
        if (value is not { } v || double.IsNaN(v) || double.IsInfinity(v) || v <= 0)
        {
            return null;
        }

        return Math.Round(Math.Clamp(v, MinWordsPerSecond, MaxWordsPerSecond), 2);
    }

    /// <summary>Ham hızı çözer.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="language">Dil (küçük harf).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kelime/sn ya da <c>null</c>.</returns>
    protected abstract Task<double?> ResolveAsync(Guid userId, string language, CancellationToken cancellationToken);
}
