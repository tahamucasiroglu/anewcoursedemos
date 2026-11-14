using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Domain.Rules;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Giriş (spec 0002 Req 5–8): kayıtlı olmayan e-posta genel hata verir (sayaç ve olay yok); kilitli hesap doğru şifrede
/// bile reddedilir ve kilit değişmez; yanlış şifre atomik sayılır, 3. hatada 15 dakika kilit; başarı sayacı sıfırlar ve
/// son giriş zamanını yazar.
/// </summary>
/// <param name="dependencies">Ortak bağımlılıklar.</param>
public abstract class LoginServiceBase(AuthServiceDependencies dependencies) : AuthServiceBase(dependencies), ILoginService
{
    /// <inheritdoc />
    public async Task<Result<AuthSessionDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = Now;
        var users = Dependencies.Users;
        var user = await users.FindByNormalizedEmailAsync(EmailNormalizer.Normalize(request.Email), cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result<AuthSessionDto>.Failure(IdentityErrors.InvalidCredentials);
        }

        if (user.IsLocked(now))
        {
            await WriteEventAsync(IdentityEvents.LoginFailed, SystemEventLevel.Warning, user.Id, cancellationToken).ConfigureAwait(false);
            return Result<AuthSessionDto>.Failure(IdentityErrors.Locked(user.LockedUntil!.Value));
        }

        if (!Dependencies.Passwords.Verify(user.PasswordHash, request.Password ?? string.Empty))
        {
            var outcome = await users.RegisterFailedAttemptAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
            await WriteEventAsync(IdentityEvents.LoginFailed, SystemEventLevel.Warning, user.Id, cancellationToken).ConfigureAwait(false);
            if (LockoutPolicy.IsLocked(outcome.LockedUntil, now))
            {
                if (outcome.Applied)
                {
                    await WriteEventAsync(IdentityEvents.Locked, SystemEventLevel.Warning, user.Id, cancellationToken).ConfigureAwait(false);
                }

                return Result<AuthSessionDto>.Failure(IdentityErrors.Locked(outcome.LockedUntil!.Value));
            }

            return Result<AuthSessionDto>.Failure(IdentityErrors.WrongPassword(LockoutPolicy.RemainingAttempts(outcome.FailedCount)));
        }

        if (!await users.RegisterSuccessfulLoginAsync(user.Id, now, cancellationToken).ConfigureAwait(false))
        {
            // Doğru şifre ama hesap bu arada kilitlendi: kilitli hesap yolundaki gibi başarısız giriş olayı yazılır (review R6-1).
            await WriteEventAsync(IdentityEvents.LoginFailed, SystemEventLevel.Warning, user.Id, cancellationToken).ConfigureAwait(false);
            var current = await users.FindByIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
            return Result<AuthSessionDto>.Failure(IdentityErrors.Locked(current?.LockedUntil ?? now + LockoutPolicy.LockDuration));
        }

        await WriteEventAsync(IdentityEvents.LoginSucceeded, SystemEventLevel.Info, user.Id, cancellationToken).ConfigureAwait(false);
        return Result<AuthSessionDto>.Success(await OpenSessionAsync(user, now, cancellationToken).ConfigureAwait(false));
    }
}
