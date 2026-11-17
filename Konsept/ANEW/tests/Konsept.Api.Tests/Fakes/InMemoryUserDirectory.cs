using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Identity.Contracts.Dtos;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Bellek kullanıcı deposundan kullanıcı dizini (hızlı profil; gerçek sorgu entegrasyon testinde).</summary>
/// <param name="users">Bellek kullanıcı deposu.</param>
public sealed class InMemoryUserDirectory(InMemoryUserRepository users) : IUserDirectory
{
    /// <inheritdoc />
    public Task<IReadOnlyList<UserDirectoryEntry>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserDirectoryEntry>>([.. users.Users.Select(u => new UserDirectoryEntry(u.Id, u.Name, u.Email, u.RegisteredAt, u.IsBanned))]);
}
