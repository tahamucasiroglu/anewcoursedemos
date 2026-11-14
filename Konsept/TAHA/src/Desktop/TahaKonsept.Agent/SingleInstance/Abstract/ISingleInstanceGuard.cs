namespace TahaKonsept.Agent.SingleInstance.Abstract;

/// <summary>
/// Uygulamanın tek örnek çalışmasını sağlar: <see cref="TryAcquire"/> ilk örnekse <c>true</c> döner; ikinci örnek
/// <see cref="SignalExistingInstance"/> ile çalışan örneğe "öne gel" der ve kapanır. Çalışan örnek <see cref="StartListening"/>
/// sonrası bu isteği <see cref="ActivationRequested"/> olayıyla alır.
/// </summary>
public interface ISingleInstanceGuard : IDisposable
{
    /// <summary>Başka örnek "pencereni göster" isteği gönderince tetiklenir (arka plan iş parçacığında).</summary>
    event EventHandler? ActivationRequested;

    /// <summary>Tek örnek kilidini almaya çalışır.</summary>
    /// <returns>Bu süreç ilk örnekse <c>true</c>.</returns>
    bool TryAcquire();

    /// <summary>Çalışan ilk örneğe öne gelme isteği gönderir.</summary>
    void SignalExistingInstance();

    /// <summary>Öne gelme isteklerini dinlemeye başlar (yalnızca kilidi alan örnek).</summary>
    void StartListening();
}
