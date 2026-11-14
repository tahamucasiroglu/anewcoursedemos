namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Orkestratörü uyandırma sinyali (singleton): servisler ve ajan olayları <see cref="Notify"/> çağırır, BackgroundService <see cref="WaitAsync"/> ile bekler.
/// </summary>
public interface IProductionSignal
{
    /// <summary>Orkestratörü uyandırır.</summary>
    void Notify();

    /// <summary>Sinyal ya da zaman aşımına kadar bekler.</summary>
    /// <param name="timeout">Azami bekleme.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
