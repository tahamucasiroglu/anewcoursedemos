using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Rules;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Kayıt (spec 0002 Req 1–4): e-posta normalleştirilmiş biçimiyle tekildir (eşzamanlı kayıtta tekil indeks karar verir),
/// şifre hash'lenir, kayıt zamanı yazılır, kullanıcı giriş yapmış sayılır ve <c>identity.registered</c> olayı yazılır.
/// </summary>
/// <param name="dependencies">Ortak bağımlılıklar.</param>
public abstract class RegistrationServiceBase(AuthServiceDependencies dependencies) : AuthServiceBase(dependencies), IRegistrationService
{
    /// <inheritdoc />
    public async Task<Result<AuthSessionDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = Now;
        var normalized = EmailNormalizer.Normalize(request.Email);
        if (await Dependencies.Users.FindByNormalizedEmailAsync(normalized, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Result<AuthSessionDto>.Failure(IdentityErrors.EmailTaken);
        }

        var user = User.Register(request.Name!, request.Email!, Dependencies.Passwords.Hash(request.Password!), now);
        if (!await Dependencies.Users.TryAddAsync(user, cancellationToken).ConfigureAwait(false))
        {
            return Result<AuthSessionDto>.Failure(IdentityErrors.EmailTaken);
        }

        // Önce oturum, sonra kayıt olayı (review R10-1): oturum kaydı düşerse Günlük'e yanlış başarı yazılmaz. Kullanıcı + oturumun
        // tek işlemde (atomik) yazılması backlog B-48.
        var session = await OpenSessionAsync(user, now, cancellationToken).ConfigureAwait(false);
        await WriteEventAsync(IdentityEvents.Registered, SystemEventLevel.Info, user.Id, cancellationToken).ConfigureAwait(false);
        return Result<AuthSessionDto>.Success(session);
    }
}
