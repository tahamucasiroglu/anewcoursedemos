namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Orkestratörün kullanıcı başına adımı: sıranın durumuna göre etkin işi durdurur/sürdürür, bitmeyen ilk aşamayı çalıştırır (sunucu aşamalarını
/// yerinde; ajan aşamalarını gönderip olay bekler), iş yoksa bütçe kontrolünden sonra sıradaki öğeyi başlatır. Ajan çevrimdışıysa "Ajan bekleniyor".
/// </summary>
public interface IJobAdvancer
{
    /// <summary>Kullanıcının sırasını bir adım ilerletir (çağıran kullanıcı kilidini tutar).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task AdvanceUserAsync(Guid userId, CancellationToken cancellationToken);
}
