using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Domain.Rules;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Oturum yaşam döngüsü (spec 0002 Req 13, 15): refresh token atomik döner (eski token ikinci kez çalışmaz, plan Risk 4);
/// her Api isteğinde oturum doğrulanır ve son kullanım en fazla dakikada bir kaydırılır; çıkış kullanıcının tüm
/// oturumlarını iptal eder ve <c>identity.logged_out</c> olayını yazar. Token değerleri asla loglanmaz.
/// </summary>
public abstract partial class SessionServiceBase : AuthServiceBase, ISessionService
{
    /// <summary>Initializes a new instance of the <see cref="SessionServiceBase"/> class.</summary>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="currentUser">İsteği yapan kullanıcı.</param>
    /// <param name="logger">Teknik log.</param>
    protected SessionServiceBase(AuthServiceDependencies dependencies, ICurrentUser currentUser, ILogger logger)
        : base(dependencies)
    {
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Gets isteği yapan kullanıcı.</summary>
    protected ICurrentUser CurrentUser { get; }

    /// <summary>Gets teknik log.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public async Task<Result<AuthSessionDto>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            return Result<AuthSessionDto>.Failure(IdentityErrors.SessionInvalid);
        }

        var now = Now;
        var factory = Dependencies.RefreshTokens;
        var next = factory.Create();
        var session = await Dependencies.Sessions
            .RotateAsync(factory.Hash(request.RefreshToken), factory.Hash(next), now, cancellationToken)
            .ConfigureAwait(false);
        if (session is null)
        {
            LogRefreshRejected(Logger);
            return Result<AuthSessionDto>.Failure(IdentityErrors.SessionInvalid);
        }

        var user = await Dependencies.Users.FindByIdAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        return user is null
            ? Result<AuthSessionDto>.Failure(IdentityErrors.SessionInvalid)
            : Result<AuthSessionDto>.Success(CreateSession(user, session.Id, next, now));
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var now = Now;
        var session = await Dependencies.Sessions.FindAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null || !session.IsActive(now))
        {
            return false;
        }

        if (SessionPolicy.ShouldTouch(session.LastUsedAt, now))
        {
            await Dependencies.Sessions.TouchAsync(sessionId, now, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<Result<int>> LogoutAsync(CancellationToken cancellationToken)
    {
        if (CurrentUser.UserId is not { } userId)
        {
            return Result<int>.Failure(IdentityErrors.SessionInvalid);
        }

        var revoked = await RevokeAllAsync(userId, cancellationToken).ConfigureAwait(false);
        await WriteEventAsync(IdentityEvents.LoggedOut, SystemEventLevel.Info, userId, cancellationToken).ConfigureAwait(false);
        return Result<int>.Success(revoked);
    }

    /// <inheritdoc />
    public async Task<Result<CurrentUserDto>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        if (CurrentUser.UserId is not { } userId)
        {
            return Result<CurrentUserDto>.Failure(IdentityErrors.SessionInvalid);
        }

        var user = await Dependencies.Users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return user is null
            ? Result<CurrentUserDto>.Failure(IdentityErrors.SessionInvalid)
            : Result<CurrentUserDto>.Success(new CurrentUserDto(user.Id, user.Name, user.Email));
    }

    /// <inheritdoc />
    public Task<int> RevokeAllAsync(Guid userId, CancellationToken cancellationToken) =>
        Dependencies.Sessions.RevokeAllAsync(userId, Now, cancellationToken);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Refresh reddedildi: token geçersiz, kullanılmış ya da oturum kapalı")]
    private static partial void LogRefreshRejected(ILogger logger);
}
