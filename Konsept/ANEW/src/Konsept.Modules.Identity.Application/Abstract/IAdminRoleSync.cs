namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Açılış rol senkronu (spec 0011 Req 5, plan Risk 2): ayardaki admin e-postası tek doğruluk kaynağıdır — eşleşen hesap
/// admin olur, diğer her hesap kullanıcıya döner; ayar boşsa kimse admin değildir.
/// </summary>
public interface IAdminRoleSync
{
    /// <summary>Rolleri ayara göre eşitler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Rolü değişen hesap sayısı.</returns>
    Task<int> SyncAsync(CancellationToken cancellationToken);
}
