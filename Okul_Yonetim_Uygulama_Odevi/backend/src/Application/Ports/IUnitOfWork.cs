namespace OkulYonetim.Application.Ports;

/// <summary>
/// Bekleyen tüm değişiklikleri tek işlemde yazar. Aynı kaydı başka bir istek önce değiştirdiyse
/// <c>ConcurrencyConflictException</c> fırlatır ve bekleyen değişiklikleri bırakır (çağıran yeniden deneyebilir).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);

    void DiscardChanges();

    /// <summary>
    /// Anahtar başına sıraya sokulmuş işlem açar (spec 0004 review H-1: "önce kontrol et, sonra yaz" akışları — kilit sayacı,
    /// yenileme anahtarı — eşzamanlı isteklerde atlatılamasın). Aynı anahtarla gelen istek, öncekinin işlemi bitene kadar bekler;
    /// en fazla 5 sn — sıra gelmezse <c>ExclusiveLockTimeoutException</c> (review I-4).
    /// </summary>
    Task<IExclusiveScope> BeginExclusiveAsync(string key, CancellationToken cancellationToken);
}
