using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>Kayıt (spec 0002 Req 1–4): hesap açar ve kullanıcıyı giriş yapmış sayar.</summary>
public interface IRegistrationService
{
    /// <summary>Yeni hesap açar ve oturum başlatır.</summary>
    /// <param name="request">Doğrulanmış kayıt isteği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oturum ya da <c>identity.email_taken</c>.</returns>
    Task<Result<AuthSessionDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
}
