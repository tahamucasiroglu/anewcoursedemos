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
/// Rol yalnızca <see cref="AdminEmailPolicy"/>'den çıkar (spec 0011 Req 5): ayardaki e-postayla kayıt olan admin, diğer
/// herkes kullanıcıdır; istekteki hiçbir alan rolü etkilemez (<see cref="RegisterRequest"/>'te rol alanı yoktur).
/// </summary>
/// <param name="dependencies">Ortak bağımlılıklar.</param>
/// <param name="options">Identity ayarları (ilk admin e-postası); verilmezse kimse admin olmaz.</param>
public abstract class RegistrationServiceBase(AuthServiceDependencies dependencies, IdentityOptions? options = null)
    : AuthServiceBase(dependencies), IRegistrationService
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

        var role = AdminEmailPolicy.RoleForNormalized(AdminEmailPolicy.NormalizedAdminEmail(options?.AdminEmail), normalized);
        var user = User.Register(request.Name!, request.Email!, Dependencies.Passwords.Hash(request.Password!), now, role);
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
