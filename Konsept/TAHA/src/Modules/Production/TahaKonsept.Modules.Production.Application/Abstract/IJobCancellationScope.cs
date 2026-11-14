namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// <see cref="IJobCancellationRegistry.Begin"/> sonucu: işe bağlı iptal belirteci; dispose edilince kayıt kaldırılır.
/// </summary>
public interface IJobCancellationScope : IDisposable
{
    /// <summary>Üst belirteç ya da iptal isteğiyle iptal olan belirteç.</summary>
    CancellationToken Token { get; }

    /// <summary>İptal, kullanıcının iptal isteğinden mi geldi (uygulama durdurma değil).</summary>
    bool CancelledByRequest { get; }
}
