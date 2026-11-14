namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Kullanıcı başına işlem kilidi (singleton): orkestratör adımı, ajan olayları ve iptal aynı kullanıcının işini aynı anda değiştirmesin.
/// </summary>
public interface IUserJobLock
{
    /// <summary>Kilidi alır (bırakmak için dispose).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<IDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Kilit boştaysa hemen alır, değilse <c>null</c>.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Sonuç.</returns>
    IDisposable? TryAcquire(Guid userId);
}
