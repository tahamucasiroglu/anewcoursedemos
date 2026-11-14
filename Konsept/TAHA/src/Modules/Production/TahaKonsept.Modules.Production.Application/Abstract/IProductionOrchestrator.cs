namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Orkestratör adımı: sırası çalışan/duraklatılmış ya da etkin işi olan her kullanıcı için (kullanıcı kilidi altında) <see cref="IJobAdvancer"/>'ı çağırır.
/// BackgroundService her tetiklemede/periyotta bir kez çağırır.
/// </summary>
public interface IProductionOrchestrator
{
    /// <summary>Bir tur çalıştırır; işlenen kullanıcı sayısını döner.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<int> TickAsync(CancellationToken cancellationToken);
}
