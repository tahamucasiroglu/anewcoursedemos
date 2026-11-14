using TahaKonsept.Modules.Concepts.Application.Base;
using TahaKonsept.Modules.Production.Application.Abstract;

namespace TahaKonsept.Modules.Production.Infrastructure.Integration.Concrete;

/// <summary>
/// Modüller arası adaptör: Concepts'in <c>INarrationPaceSource</c> portunu Production'ın <see cref="ISpeechRateService"/>'i ile karşılar —
/// konsept anlatımı, kullanıcının üretimde seçtiği ses modelinin (ölçülmüş ya da katalog) hızıyla boyutlanır. Scoped kaydedilir;
/// Concepts'in <c>DefaultNarrationPaceSource</c> yedeğinin yerini alır (<c>Replace</c>).
/// </summary>
public sealed class ProductionNarrationPaceSource : NarrationPaceSourceBase
{
    private readonly ISpeechRateService _rates;

    /// <summary>Oluşturur.</summary>
    /// <param name="rates">Konuşma hızı servisi.</param>
    public ProductionNarrationPaceSource(ISpeechRateService rates)
    {
        _rates = rates ?? throw new ArgumentNullException(nameof(rates));
    }

    /// <summary>Kullanıcının üretim ayarlarındaki ses modeline göre hız.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="language">Dil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kelime/sn ya da <c>null</c>.</returns>
    protected override Task<double?> ResolveAsync(Guid userId, string language, CancellationToken cancellationToken) =>
        _rates.GetForUserAsync(userId, language, cancellationToken);
}
