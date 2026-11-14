using System.Linq.Expressions;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Paging;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Modules.Diagnostics.Application.Models;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Concrete;

/// <summary>
/// <see cref="IActivityLogService"/> uygulaması: şerit için son kayıtlar, güne/seviyeye/kaynağa göre sayfalı arama ve istemci olayı yazma.
/// Yönetici herkesin kayıtlarını, diğer kullanıcılar kendi + sistem kayıtlarını görür.
/// </summary>
public sealed class ActivityLogService : DiagnosticsServiceBase, IActivityLogService
{
    private const int DefaultRecentTake = 50;
    private readonly IActivityLogRepository _logs;

    /// <summary>Oluşturur.</summary>
    /// <param name="logs">Günlük deposu.</param>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    public ActivityLogService(IActivityLogRepository logs, ICurrentUser currentUser)
        : base(currentUser)
    {
        _logs = logs ?? throw new ArgumentNullException(nameof(logs));
    }

    /// <summary>Son kayıtlar (eskiden yeniye).</summary>
    /// <param name="afterId">Son görülen kayıt.</param>
    /// <param name="take">En çok kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    public async Task<Result<IReadOnlyList<ActivityLogDto>>> GetRecentAsync(Guid? afterId, int? take, CancellationToken cancellationToken = default)
    {
        var items = await _logs.GetRecentAsync(afterId, ClampTake(take, DefaultRecentTake), OwnerScope, cancellationToken);
        return Result.Success<IReadOnlyList<ActivityLogDto>>(items.Select(l => l.ToDto()).ToList());
    }

    /// <summary>Filtreli, sayfalı arama.</summary>
    /// <param name="query">Filtre.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    public async Task<Result<PagedResult<ActivityLogDto>>> SearchAsync(ActivityLogQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = await _logs.GetPagedAsync(
            new PagedRequest { Page = query.Page, PageSize = ClampTake(query.PageSize, 50) },
            BuildPredicate(query, OwnerScope),
            q => q.OrderByDescending(l => l.CreatedAtUtc).ThenByDescending(l => l.Id),
            cancellationToken);
        return page.Map(l => l.ToDto());
    }

    /// <summary>İstemci olayını yazar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oluşan kayıt.</returns>
    public async Task<Result<ActivityLogDto>> CreateAsync(ActivityLogCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = ActivityLog.Create(
            CurrentUserIdOrEmpty,
            request.Level,
            string.IsNullOrWhiteSpace(request.Source) ? ActivitySources.Web : request.Source,
            request.Message,
            ActivityDetailsJson.Serialize(request.Details),
            request.DurationMs);
        await _logs.AddAsync(entry, cancellationToken);
        await _logs.UnitOfWork.SaveChangesAsync(cancellationToken);
        return entry.ToDto();
    }

    private static Expression<Func<ActivityLog, bool>> BuildPredicate(ActivityLogQuery query, Guid? owner)
    {
        DateTimeOffset? from = null;
        DateTimeOffset? to = null;
        if (query.Day is { } day)
        {
            (from, to) = DiagnosticsTime.DayRangeUtc(day);
        }

        var level = query.Level;
        var source = string.IsNullOrWhiteSpace(query.Source) ? null : query.Source.Trim();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim().ToLowerInvariant();
        var hasOwner = owner.HasValue;
        var ownerId = owner ?? Guid.Empty;

        return l => (!hasOwner || l.UserId == ownerId || l.UserId == Guid.Empty)
            && (from == null || l.CreatedAtUtc >= from)
            && (to == null || l.CreatedAtUtc < to)
            && (level == null || l.Level == level)
            && (source == null || l.Source == source)
            && (search == null || l.Message.ToLower().Contains(search));
    }
}
