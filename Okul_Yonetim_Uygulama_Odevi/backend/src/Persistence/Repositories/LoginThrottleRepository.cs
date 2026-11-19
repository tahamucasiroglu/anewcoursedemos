using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Hatalı giriş sayacı deposu.</summary>
public sealed class LoginThrottleRepository : ILoginThrottleRepository
{
    private readonly OkulDbContext context;

    public LoginThrottleRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<LoginThrottle?> FindAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        this.context.LoginThrottles.FirstOrDefaultAsync(t => t.Email == normalizedEmail, cancellationToken);

    public void Add(LoginThrottle throttle) => this.context.LoginThrottles.Add(throttle);
}
