namespace Konsept.Modules.Concepts.Contracts.Abstract;

/// <summary>
/// Yeniden üret bağlamı (spec 0020 Req 4, 15): Üretilen Videolar'daki Yeniden üret eylemi kullanıcı + konsept için kısa ömürlü bir belirteç
/// açar; Concepts düzenleme döngüsü (0015) yalnızca bu belirteçle gelen istekte "Video oldu" konsepti kaydeder, yeniden düzenletir ve
/// revizyonu onaylar. Belirteçsiz istek 0015'teki gibi reddedilir. Bellekte tutulur (Api yeniden başlarsa bağlam yeniden açılır).
/// </summary>
public interface IConceptRegenerationContext
{
    /// <summary>Bağlam açar (öncekinin yerini alır).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <returns>Belirteç.</returns>
    string Open(Guid userId, Guid conceptId);

    /// <summary>Belirteç bu kullanıcı + konsept için açık mı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="token">Belirteç.</param>
    /// <returns>Açıksa <c>true</c>.</returns>
    bool IsOpen(Guid userId, Guid conceptId, string? token);

    /// <summary>Bağlamı kapatır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    void Close(Guid userId, Guid conceptId);
}
