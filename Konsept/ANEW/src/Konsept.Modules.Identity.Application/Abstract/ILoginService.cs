using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>Giriş (spec 0002 Req 5–8): şifre doğrulama, 3 hak ve 15 dakikalık kilit.</summary>
public interface ILoginService
{
    /// <summary>E-posta ve şifreyle giriş yapar.</summary>
    /// <param name="request">Doğrulanmış giriş isteği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oturum ya da kimlik hatası (hatalı kimlik, yanlış şifre + kalan hak, kilit).</returns>
    Task<Result<AuthSessionDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
