namespace Konsept.Modules.Identity.Contracts.Abstract;

/// <summary>
/// Kullanıcının tüm web oturumları kapandığında (çıkış — 0002 Req 15 —, ileride ban 0024) Identity'nin haber verdiği dinleyici
/// (spec 0006 Req 16, 21). Production uygular: çalışan işi durdurur. Identity Production'a bağımlı olmasın diye arayüz buradadır.
/// </summary>
public interface IUserSessionsEndedListener
{
    /// <summary>Kullanıcının son geçerli oturumu kapandı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task OnAllSessionsEndedAsync(Guid userId, CancellationToken cancellationToken);
}
