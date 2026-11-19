using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Ports;

/// <summary>Kişiler (kurumdan bağımsız tek kimlik). E-posta karşılaştırma biçimiyle aranır.</summary>
public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(User user);
}
