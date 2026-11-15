using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="IActivityLogRepository"/> EF uygulaması: "son kayıtlar" (an + kimlik sıralı) ve seviyeye göre gruplu sayım.
/// </summary>
public sealed class ActivityLogRepository : Repository<ActivityLog>, IActivityLogRepository
{
    /// <summary>Oluşturur.</summary>
    /// <param name="context">Diagnostics DbContext'i.</param>
    public ActivityLogRepository(DiagnosticsDbContext context)
        : base(context)
    {
    }

    /// <summary>Son kayıtlar (eskiden yeniye).</summary>
    /// <param name="afterId">Son görülen kayıt.</param>
    /// <param name="take">En çok kayıt.</param>
    /// <param name="ownerId">Kapsam (<c>null</c> → hepsi).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    public async Task<IReadOnlyList<ActivityLog>> GetRecentAsync(Guid? afterId, int take, Guid? ownerId, CancellationToken cancellationToken = default)
    {
        var query = QueryNoTracking();
        if (ownerId is { } owner)
        {
            // Sistem (Guid.Empty) kayıtları yalnız yöneticiye: başka kullanıcıların işlerinden bilgi taşıyabilir.
            query = owner == Guid.Empty ? query.Where(_ => false) : query.Where(l => l.UserId == owner);
        }

        if (afterId is { } anchorId)
        {
            // Çapa da kapsam içinde aranır: başkasının kayıt kimliği çapa olarak verilirse yok sayılır.
            var anchor = await query
                .Where(l => l.Id == anchorId)
                .Select(l => (DateTimeOffset?)l.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (anchor is { } since)
            {
                // Aynı anda yazılmış kayıtlar da gelsin (istemci kimliğe göre tekilleştirir).
                query = query.Where(l => l.CreatedAtUtc >= since && l.Id != anchorId);
            }
        }

        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .ThenByDescending(l => l.Id)
            .Take(Math.Max(take, 1))
            .ToListAsync(cancellationToken);
        items.Reverse();
        return items;
    }

    /// <summary>Seviyeye göre sayım.</summary>
    /// <param name="sinceUtc">Başlangıç (<c>null</c> → tümü).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Seviye → sayı.</returns>
    public async Task<IReadOnlyDictionary<LogLevelKind, int>> CountByLevelAsync(DateTimeOffset? sinceUtc, CancellationToken cancellationToken = default)
    {
        var query = QueryNoTracking();
        if (sinceUtc is { } since)
        {
            query = query.Where(l => l.CreatedAtUtc >= since);
        }

        var rows = await query
            .GroupBy(l => l.Level)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.Level, r => r.Count);
    }
}
