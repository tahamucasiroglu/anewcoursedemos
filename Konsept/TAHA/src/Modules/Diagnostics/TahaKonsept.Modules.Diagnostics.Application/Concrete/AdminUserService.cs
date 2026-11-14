using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Diagnostics.Domain.Errors;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Concrete;

/// <summary>
/// <see cref="IAdminUserService"/> uygulaması: <see cref="IAdminUserDirectory"/> üzerinden liste / yasakla / yasağı kaldır.
/// Dizin bağlı değilse liste boş döner, yasaklama "kullanılamıyor" hatası verir. Yasak işlemleri günlüğe uyarı olarak düşer.
/// </summary>
public sealed class AdminUserService : DiagnosticsServiceBase, IAdminUserService
{
    private readonly IAdminUserDirectory _directory;
    private readonly IActivityLogger _activity;

    /// <summary>Oluşturur.</summary>
    /// <param name="directory">Kullanıcı dizini.</param>
    /// <param name="activity">İşlem günlüğü.</param>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    public AdminUserService(IAdminUserDirectory directory, IActivityLogger activity, ICurrentUser currentUser)
        : base(currentUser)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <summary>Kullanıcıları listeler (yönetici).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    public async Task<Result<IReadOnlyList<AdminUserDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (RequireAdmin() is { } denied)
        {
            return denied;
        }

        var users = await _directory.ListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<AdminUserDto>>(users.OrderBy(u => u.UserName, StringComparer.CurrentCultureIgnoreCase).ToList());
    }

    /// <summary>Kullanıcıyı yasaklar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> BanAsync(Guid userId, CancellationToken cancellationToken = default) => SetBannedAsync(userId, true, cancellationToken);

    /// <summary>Yasağı kaldırır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> UnbanAsync(Guid userId, CancellationToken cancellationToken = default) => SetBannedAsync(userId, false, cancellationToken);

    private async Task<Result> SetBannedAsync(Guid userId, bool banned, CancellationToken cancellationToken)
    {
        if (RequireAdmin() is { } denied)
        {
            return denied;
        }

        if (banned && CurrentUser.UserId == userId)
        {
            return DiagnosticsErrors.CannotBanSelf;
        }

        if (!_directory.IsAvailable)
        {
            return DiagnosticsErrors.UserDirectoryUnavailable;
        }

        var result = await _directory.SetBannedAsync(userId, banned, cancellationToken);
        if (result.IsSuccess)
        {
            await _activity.WarningAsync(
                ActivitySources.Diagnostics,
                banned ? "Kullanıcı yasaklandı." : "Kullanıcının yasağı kaldırıldı.",
                new { userId },
                cancellationToken: cancellationToken);
        }

        return result;
    }
}
