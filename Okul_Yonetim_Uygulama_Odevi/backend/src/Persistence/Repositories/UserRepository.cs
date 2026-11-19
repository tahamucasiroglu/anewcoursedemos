using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Kişi deposu (kurumdan bağımsız tablo; kurum filtresi yoktur).</summary>
public sealed class UserRepository : IUserRepository
{
    private readonly OkulDbContext context;

    public UserRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        this.context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public void Add(User user) => this.context.Users.Add(user);
}
