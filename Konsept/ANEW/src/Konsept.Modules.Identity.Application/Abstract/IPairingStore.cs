using Konsept.Modules.Identity.Domain.Entities;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Eşleştirme kodlarının deposu (spec 0012, plan Risk 2: bellekte, 10 dk ömür). Verilen işlem depo kilidi altında çalışır;
/// onay/ret/teslim geçişleri böylece atomiktir (çift tıklama iki kez onaylamaz, AC-10).
/// </summary>
public interface IPairingStore
{
    /// <summary>Kod kullanımda mı (yeni kodun benzersizliği için).</summary>
    /// <param name="code">Kod.</param>
    /// <returns>Kullanımdaysa <c>true</c>.</returns>
    bool Contains(string code);

    /// <summary>İsteği ekler; süresi çoktan dolmuş eski istekleri atar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="now">Şimdiki zaman.</param>
    void Add(PairingRequest request, DateTimeOffset now);

    /// <summary>Koda göre isteği bulur ve işlemi kilit altında çalıştırır.</summary>
    /// <typeparam name="T">Sonuç tipi.</typeparam>
    /// <param name="code">Kanonik kod.</param>
    /// <param name="action">İşlem (istek yoksa <c>null</c> alır).</param>
    /// <returns>İşlemin sonucu.</returns>
    T WithCode<T>(string code, Func<PairingRequest?, T> action);

    /// <summary>Onay sırrının hash'ine göre isteği bulur ve işlemi kilit altında çalıştırır.</summary>
    /// <typeparam name="T">Sonuç tipi.</typeparam>
    /// <param name="secretHash">Sırrın hash'i.</param>
    /// <param name="action">İşlem (istek yoksa <c>null</c> alır).</param>
    /// <returns>İşlemin sonucu.</returns>
    T WithSecretHash<T>(string secretHash, Func<PairingRequest?, T> action);
}
