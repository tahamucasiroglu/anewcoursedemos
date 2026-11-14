using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Rules;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Hızlı profil için bellekte kullanıcı deposu. Her işlem tek kilit altında yapılır: gerçek deponun atomik SQL'inin
/// eşdeğeri (sayaç kuralı <see cref="LockoutPolicy"/>'den). Gerçek PostgreSQL davranışı <c>IdentityIntegrationTests</c>'te.
/// </summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly Lock _gate = new();
    private readonly List<User> _users = [];

    /// <summary>Gets kayıtlı kullanıcıların anlık kopyası.</summary>
    public IReadOnlyList<User> Users
    {
        get
        {
            lock (_gate)
            {
                return [.. _users];
            }
        }
    }

    /// <summary>E-postaya göre kullanıcıyı getirir (test doğrulamaları için).</summary>
    /// <param name="email">E-posta.</param>
    /// <returns>Kullanıcı.</returns>
    public User Get(string email)
    {
        var normalized = EmailNormalizer.Normalize(email);
        return Users.Single(u => u.NormalizedEmail == normalized);
    }

    /// <inheritdoc />
    public Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.NormalizedEmail == normalizedEmail));
        }
    }

    /// <inheritdoc />
    public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == userId));
        }
    }

    /// <inheritdoc />
    public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        lock (_gate)
        {
            if (_users.Any(u => u.NormalizedEmail == user.NormalizedEmail))
            {
                return Task.FromResult(false);
            }

            _users.Add(user);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<LockoutOutcome> RegisterFailedAttemptAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_users.Single(u => u.Id == userId).ApplyFailedAttempt(now));
        }
    }

    /// <inheritdoc />
    public Task<bool> RegisterSuccessfulLoginAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_users.Single(u => u.Id == userId).ApplySuccessfulLogin(now));
        }
    }
}
