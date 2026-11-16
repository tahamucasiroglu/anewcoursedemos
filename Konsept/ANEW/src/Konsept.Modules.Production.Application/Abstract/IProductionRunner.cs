using Konsept.Modules.AiGateway.Contracts.Dtos;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>
/// Kullanıcı başına üretim koşucusu (spec 0017 Req 5, 8, 9): istekten bağımsız çalışır (tarayıcı kapansa da sürer), aynı anda kullanıcı başına
/// tek döngü. Sırayla üret / Devam et çağıranın kimliğini (yetkiler dahil; istekte sunucuda yakalanır) verir; Durdur/çıkış süren yerel aşamayı
/// keser. Kimlik Api belleğindedir (Api yeniden başlarsa çalışan iş Durduruldu olur, B-120).
/// </summary>
public interface IProductionRunner
{
    /// <summary>Kullanıcının döngüsünü bu kimlikle başlatır (çalışıyorsa yeniden değerlendirir).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="caller">Çağıran kimliği.</param>
    void Launch(Guid userId, ModelCallerIdentity caller);

    /// <summary>Kimliği bilinen kullanıcının döngüsünü yeniden değerlendirir (ör. ajan bağlandı).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Kimlik biliniyorsa <c>true</c>.</returns>
    bool Nudge(Guid userId);

    /// <summary>Süren aşamayı keser (Durdur, çıkış).</summary>
    /// <param name="userId">Kullanıcı.</param>
    void Cancel(Guid userId);

    /// <summary>Kullanıcının döngüsü çalışıyor mu.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Çalışıyorsa <c>true</c>.</returns>
    bool IsBusy(Guid userId);

    /// <summary>
    /// Çalışan işleri tarar: açılışta kimliği bilinmeyen çalışan işler Durduruldu olur (Api yeniden başladı; B-120); sonra her taramada
    /// kimliği bilinen kullanıcıların döngüsü yeniden değerlendirilir (ör. Ajan bekleniyor işi, ajan bağlanınca sürer — Req 15).
    /// </summary>
    /// <param name="startup">Açılış taraması mı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task SweepAsync(bool startup, CancellationToken cancellationToken);
}
