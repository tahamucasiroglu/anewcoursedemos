namespace TahaKonsept.Agent.Threading.Abstract;

/// <summary>
/// Görünüm modellerinin UI iş parçacığına iş göndermesi (arka plan olayları → bağlı özellikler). Testlerde anında çalıştıran sahte kullanılır.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>İşi UI iş parçacığında çalıştırır (zaten oradaysa hemen).</summary>
    /// <param name="action">İş.</param>
    void Post(Action action);
}
