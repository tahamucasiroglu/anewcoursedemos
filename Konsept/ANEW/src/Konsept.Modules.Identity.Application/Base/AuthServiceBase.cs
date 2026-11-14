using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Kayıt, giriş ve oturum servislerinin ortak temeli (K-5): oturum açma (refresh token + hash + oturum kaydı + JWT) ve
/// kullanıcıya bağlı Günlük olayı yazma. Olay metni şifre ya da token içermez.
/// </summary>
public abstract class AuthServiceBase
{
    /// <summary>Initializes a new instance of the <see cref="AuthServiceBase"/> class.</summary>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    protected AuthServiceBase(AuthServiceDependencies dependencies)
    {
        Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
    }

    /// <summary>Gets ortak bağımlılıklar.</summary>
    protected AuthServiceDependencies Dependencies { get; }

    /// <summary>Gets şimdiki zaman (UTC).</summary>
    protected DateTimeOffset Now => Dependencies.TimeProvider.GetUtcNow();

    /// <summary>Kullanıcı için yeni oturum açar ve token çiftini döndürür.</summary>
    /// <param name="user">Kullanıcı.</param>
    /// <param name="now">Zaman.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Token çifti.</returns>
    protected async Task<AuthSessionDto> OpenSessionAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        var refreshToken = Dependencies.RefreshTokens.Create();
        var session = UserSession.Open(user.Id, Dependencies.RefreshTokens.Hash(refreshToken), now);
        await Dependencies.Sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
        return CreateSession(user, session.Id, refreshToken, now);
    }

    /// <summary>Mevcut oturum için token çifti oluşturur (yeni JWT).</summary>
    /// <param name="user">Kullanıcı.</param>
    /// <param name="sessionId">Oturum.</param>
    /// <param name="refreshToken">Refresh token.</param>
    /// <param name="now">Zaman.</param>
    /// <returns>Token çifti.</returns>
    protected AuthSessionDto CreateSession(User user, Guid sessionId, string refreshToken, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(user);
        var access = Dependencies.Tokens.Issue(user.Id, user.Name, sessionId, now);
        return new AuthSessionDto(access.Token, access.ExpiresAt, refreshToken, user.Id, user.Name);
    }

    /// <summary>Kullanıcıya bağlı Günlük olayı yazar.</summary>
    /// <param name="type">Olay türü (<see cref="IdentityEvents"/>).</param>
    /// <param name="level">Seviye.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected Task WriteEventAsync(string type, SystemEventLevel level, Guid userId, CancellationToken cancellationToken) =>
        Dependencies.Events.WriteAsync(new SystemEventEntry(type, level, IdentityEvents.MessageOf(type), userId), cancellationToken);
}
