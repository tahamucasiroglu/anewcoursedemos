namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Kurulum kaydı ve kaldırmanın Windows tarafı (spec 0023 Req 10–11): yalnızca geçerli kullanıcının kayıt defteri (HKCU), Başlat menüsü kısayolu
/// ve süreçler — yönetici izni (UAC) gerektiren hiçbir işlem yoktur. Testte sahte.
/// </summary>
public interface IInstallHost
{
    /// <summary>HKCU altındaki anahtara metin değeri yazar.</summary>
    /// <param name="subKey">HKCU'ya göre alt anahtar (ör. <c>Software\Microsoft\Windows\CurrentVersion\Run</c>).</param>
    /// <param name="name">Değer adı.</param>
    /// <param name="value">Değer.</param>
    void SetUserValue(string subKey, string name, object value);

    /// <summary>HKCU altındaki değeri siler (yoksa bir şey yapmaz).</summary>
    /// <param name="subKey">Alt anahtar.</param>
    /// <param name="name">Değer adı.</param>
    void DeleteUserValue(string subKey, string name);

    /// <summary>HKCU altındaki anahtarı alt anahtarlarıyla siler (yoksa bir şey yapmaz).</summary>
    /// <param name="subKey">Alt anahtar.</param>
    void DeleteUserKey(string subKey);

    /// <summary>Kullanıcının Başlat menüsünde kısayol oluşturur.</summary>
    /// <param name="shortcutPath">Kısayol dosyası.</param>
    /// <param name="target">Hedef exe.</param>
    void CreateShortcut(string shortcutPath, string target);

    /// <summary>Exe'yi başlatır.</summary>
    /// <param name="executable">Exe.</param>
    /// <param name="arguments">Argümanlar.</param>
    void Launch(string executable, string arguments);

    /// <summary>Bu exe yolundan çalışan diğer ajan süreçlerini kapatır (kendi süreci hariç).</summary>
    /// <param name="executable">Kurulu exe.</param>
    void StopRunning(string executable);
}
