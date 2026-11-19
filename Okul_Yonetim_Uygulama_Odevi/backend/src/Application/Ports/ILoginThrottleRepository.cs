using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Ports;

/// <summary>E-posta başına hatalı giriş sayaçları; eşzamanlı güncellemede çakışma <c>ConcurrencyConflictException</c> olur.</summary>
public interface ILoginThrottleRepository
{
    Task<LoginThrottle?> FindAsync(string normalizedEmail, CancellationToken cancellationToken);

    void Add(LoginThrottle throttle);
}
