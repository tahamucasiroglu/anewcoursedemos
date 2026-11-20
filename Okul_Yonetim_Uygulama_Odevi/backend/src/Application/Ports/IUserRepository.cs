using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Ports;

/// <summary>Kişiler (kurumdan bağımsız tek kimlik). E-posta karşılaştırma biçimiyle aranır.</summary>
public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Kişinin e-postası (izlemesiz; sıra almadan önce anahtar için).</summary>
    Task<string?> FindEmailAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Kişi, veritabanından taze (bu istekte daha önce izlendiyse yeniden yüklenir) — sıra içindeki kararlar için.</summary>
    Task<User?> FindByIdFreshAsync(Guid id, CancellationToken cancellationToken);

    void Add(User user);
}
