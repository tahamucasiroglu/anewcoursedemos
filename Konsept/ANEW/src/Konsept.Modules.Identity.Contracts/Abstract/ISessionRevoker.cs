namespace Konsept.Modules.Identity.Contracts.Abstract;

/// <summary>
/// Bir kullanıcının tüm oturumlarını anında geçersiz kılar (BR-14). Çıkış (0002), ajan oturumunun düşürülmesi (0012) ve
/// ban (0024) aynı yeteneği kullanır.
/// </summary>
public interface ISessionRevoker
{
    /// <summary>Kullanıcının iptal edilmemiş tüm oturumlarını iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İptal edilen oturum sayısı.</returns>
    Task<int> RevokeAllAsync(Guid userId, CancellationToken cancellationToken);
}
