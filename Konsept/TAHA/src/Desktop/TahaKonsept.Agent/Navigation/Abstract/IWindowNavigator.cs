namespace TahaKonsept.Agent.Navigation.Abstract;

/// <summary>
/// Pencereleri açar/öne getirir (her pencereden en fazla bir tane): kurulum, durum ve "varsayılan" (kurulum gerekiyorsa kurulum,
/// değilse durum). Tepsi menüsü, tek örnek etkinleştirmesi ve başlangıç akışı kullanır. UI iş parçacığında çağrılmalıdır.
/// </summary>
public interface IWindowNavigator
{
    /// <summary>Kurulum penceresini açar.</summary>
    /// <param name="pairingOnly">Zorunlu bileşenler kuruluysa doğrudan eşleme adımıyla açılsın mı.</param>
    void ShowSetup(bool pairingOnly = false);

    /// <summary>Durum penceresini açar.</summary>
    void ShowStatus();

    /// <summary>Kurulum gerekiyorsa kurulumu, değilse durumu açar.</summary>
    void ShowDefault();

    /// <summary>Kurulum (ön gereksinim ya da eşleme) gerekiyor mu.</summary>
    /// <returns>Gerekiyorsa <c>true</c>.</returns>
    bool IsSetupRequired();

    /// <summary>Açık pencereleri kapatır (çıkışta).</summary>
    void CloseAll();
}
