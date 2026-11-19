namespace OkulYonetim.Application.Ports;

/// <summary>
/// Bekleyen tüm değişiklikleri tek işlemde yazar. Aynı kaydı başka bir istek önce değiştirdiyse
/// <c>ConcurrencyConflictException</c> fırlatır ve bekleyen değişiklikleri bırakır (çağıran yeniden deneyebilir).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);

    void DiscardChanges();
}
