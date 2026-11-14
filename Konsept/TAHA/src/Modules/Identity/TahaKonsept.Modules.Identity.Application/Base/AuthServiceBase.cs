using Microsoft.AspNetCore.Identity;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Base;

/// <summary>
/// <see cref="IAuthService"/> tabanı: kullanıcı yöneticisini tutar ve <see cref="IdentityResult"/> → <see cref="Error"/>
/// dönüşümünü (<see cref="ToError"/>) sağlar. Akışlar türetilen sınıftadır.
/// </summary>
public abstract class AuthServiceBase : IAuthService
{
    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="users">Kullanıcı yöneticisi.</param>
    protected AuthServiceBase(UserManager<AppUser> users)
    {
        Users = users ?? throw new ArgumentNullException(nameof(users));
    }

    /// <summary>Kullanıcı yöneticisi.</summary>
    protected UserManager<AppUser> Users { get; }

    /// <inheritdoc />
    public abstract Task<Result<SetupStatusResponse>> GetSetupStatusAsync(CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result<TokenResponse>> SetupAsync(SetupRequest request, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result<TokenResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result> LogoutAllAsync(CancellationToken ct = default);

    /// <summary>Başarısız Identity sonucunu doğrulama hatasına çevirir.</summary>
    /// <param name="result">Identity sonucu.</param>
    /// <returns>Hata.</returns>
    protected static Error ToError(IdentityResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return IdentityErrors.IdentityFailed(result.Errors.Select(e => e.Description));
    }
}
