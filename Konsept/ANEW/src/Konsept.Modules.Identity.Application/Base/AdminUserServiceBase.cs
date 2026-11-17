using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Admin Kullanıcılar (spec 0024). Liste bellekte süzülür ve 25'erli sayfalanır (tek Pi, az kullanıcı). Ban sırası: hedef var mı
/// (yoksa 404) → admin mi (kendisi / başka admin: reddedilir, Req 7) → sebep en fazla 500 → tek atomik koşullu güncelleme; yalnızca
/// durum değiştiyse kayıt + olay + tüm oturumların iptali (web, ajan, arka plan işleri — BR-13, Ç-4). Zaten banlıysa ikinci kayıt
/// oluşmaz. Hiçbir kullanıcı verisi silinmez.
/// </summary>
/// <param name="dependencies">Bağımlılıklar.</param>
public abstract class AdminUserServiceBase(AdminUserDependencies dependencies) : IAdminUserService
{
    /// <summary>Gets bağımlılıklar.</summary>
    protected AdminUserDependencies Dependencies { get; } = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

    private Guid AdminId => Dependencies.CurrentUser.UserId ?? Guid.Empty;

    /// <inheritdoc />
    public async Task<Result<AdminUserPageDto>> ListAsync(AdminUsersRequest? request, CancellationToken cancellationToken)
    {
        var page = UserSearch.ParsePage(request?.Page);
        var search = UserSearch.ParseSearch(request?.Search);
        var status = UserSearch.ParseStatus(request?.Status);
        var users = await Dependencies.Users.ListAsync(cancellationToken).ConfigureAwait(false);
        var matching = users
            .Where(u => (search is null || UserSearch.Matches(search, u.Name, u.Email)) && UserSearch.MatchesStatus(status, u.IsBanned))
            .OrderByDescending(u => u.RegisteredAt)
            .ThenBy(u => u.Id)
            .ToList();
        var pageUsers = matching.Skip((page - 1) * UserSearch.PageSize).Take(UserSearch.PageSize).ToList();
        var items = await ToRowsAsync(pageUsers, users, cancellationToken).ConfigureAwait(false);
        var filtered = search is not null || status != UserSearch.StatusAll;
        return Result<AdminUserPageDto>.Success(new AdminUserPageDto(items, page, UserSearch.PageSize, matching.Count, filtered));
    }

    /// <inheritdoc />
    public async Task<Result<AdminUserDto>> BanAsync(Guid userId, BanUserRequest? request, CancellationToken cancellationToken)
    {
        var target = await Dependencies.Users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            return Result<AdminUserDto>.Failure(IdentityErrors.UserNotFound);
        }

        if (target.Role == Role.Admin)
        {
            return Result<AdminUserDto>.Failure(target.Id == Dependencies.CurrentUser.UserId ? IdentityErrors.CannotBanSelf : IdentityErrors.CannotBanAdmin);
        }

        if (BanReason.IsTooLong(request?.Reason))
        {
            return Result<AdminUserDto>.Failure(IdentityErrors.BanReasonTooLong);
        }

        var now = Dependencies.TimeProvider.GetUtcNow();
        if (await Dependencies.Users.SetBannedAsync(target.Id, now, cancellationToken).ConfigureAwait(false))
        {
            // Durum değişti: ban istek iptal edilse de bütün olarak tamamlanır.
            await Dependencies.Bans.AddAsync(UserBanRecord.Create(target.Id, BanAction.Ban, AdminId, now, request?.Reason), CancellationToken.None).ConfigureAwait(false);
            await WriteEventAsync(IdentityEvents.Banned, target.Id).ConfigureAwait(false);

            // Anında etkili (Req 4): web oturumları + ajan cihazları (canlı bağlantıya oturum kapandı) + oturum sonu dinleyicileri
            // (0006 Req 16 çalışan iş Durduruldu, 0014 Req 20 süren parti durur).
            await Dependencies.Sessions.RevokeAllAsync(target.Id, CancellationToken.None).ConfigureAwait(false);
        }

        return await RowAsync(target.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<AdminUserDto>> UnbanAsync(Guid userId, CancellationToken cancellationToken)
    {
        var target = await Dependencies.Users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            return Result<AdminUserDto>.Failure(IdentityErrors.UserNotFound);
        }

        var now = Dependencies.TimeProvider.GetUtcNow();
        if (await Dependencies.Users.SetBannedAsync(target.Id, null, cancellationToken).ConfigureAwait(false))
        {
            await Dependencies.Bans.AddAsync(UserBanRecord.Create(target.Id, BanAction.Unban, AdminId, now, null), CancellationToken.None).ConfigureAwait(false);
            await WriteEventAsync(IdentityEvents.Unbanned, target.Id).ConfigureAwait(false);
        }

        return await RowAsync(target.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Olay yazılamazsa çağrılır (ban yine geçerlidir).</summary>
    /// <param name="type">Olay türü.</param>
    /// <param name="exception">Hata.</param>
    protected abstract void OnEventWriteFailed(string type, Exception exception);

    private async Task WriteEventAsync(string type, Guid userId)
    {
        try
        {
            await Dependencies.Events
                .WriteAsync(new SystemEventEntry(type, SystemEventLevel.Info, IdentityEvents.MessageOf(type), userId), CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnEventWriteFailed(type, ex);
        }
    }

    private async Task<Result<AdminUserDto>> RowAsync(Guid userId, CancellationToken cancellationToken)
    {
        var users = await Dependencies.Users.ListAsync(cancellationToken).ConfigureAwait(false);
        var user = users.FirstOrDefault(u => u.Id == userId);
        if (user is null)
        {
            return Result<AdminUserDto>.Failure(IdentityErrors.UserNotFound);
        }

        var rows = await ToRowsAsync([user], users, cancellationToken).ConfigureAwait(false);
        return Result<AdminUserDto>.Success(rows[0]);
    }

    private async Task<IReadOnlyList<AdminUserDto>> ToRowsAsync(IReadOnlyList<User> page, IReadOnlyList<User> all, CancellationToken cancellationToken)
    {
        if (page.Count == 0)
        {
            return [];
        }

        var clips = await Dependencies.Clips.CountByUserAsync(cancellationToken).ConfigureAwait(false);
        var records = await Dependencies.Bans.ListForUsersAsync([.. page.Select(u => u.Id)], cancellationToken).ConfigureAwait(false);
        var names = all.ToDictionary(u => u.Id, u => u.Name);
        var self = Dependencies.CurrentUser.UserId;
        return [.. page.Select(u => new AdminUserDto(
            u.Id,
            u.Name,
            u.Email,
            u.RegisteredAt,
            u.LastLoginAt,
            u.IsBanned ? UserSearch.StatusBanned : UserSearch.StatusActive,
            u.Role == Role.Admin ? "admin" : "user",
            clips.GetValueOrDefault(u.Id),
            u.Id == self,
            [.. records
                .Where(r => r.UserId == u.Id)
                .OrderByDescending(r => r.At)
                .ThenByDescending(r => r.Id)
                .Select(r => new AdminBanRecordDto(
                    r.Action == BanAction.Ban ? "ban" : "unban",
                    names.GetValueOrDefault(r.AdminId) ?? "—",
                    r.At,
                    r.Reason))]))];
    }
}
