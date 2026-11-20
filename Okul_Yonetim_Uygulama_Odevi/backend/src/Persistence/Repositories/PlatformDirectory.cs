using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Platform;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Platform paneli okuması (spec 0007) — izinli liste (T5): okul filtresini bilerek deler, çünkü platform yöneticisi bütün
/// okulların özetini görür. Yalnızca özet alanlar seçilir (okul adı, tarih, durum, sayılar, kurucu ad + e-posta); kişi
/// listesi, telefon, şube ayrıntısı dönmez. Çağıran taraf platform yetkisini denetler.
/// </summary>
public sealed class PlatformDirectory : IPlatformDirectory
{
    private readonly OkulDbContext context;

    public PlatformDirectory(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<SchoolPage> ListAsync(SchoolListFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        IQueryable<Tenant> query = this.context.Tenants.IgnoreQueryFilters();
        if (filter.Active is { } active)
        {
            query = query.Where(t => t.IsActive == active);
        }

        var tenants = await query.Select(t => new { t.Id, t.Name }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var matched = tenants
            .Where(t => filter.Search is not { } search || TurkishText.Contains(t.Name, search))
            .OrderBy(t => t.Name, TurkishText.IgnoreCase).ThenBy(t => t.Id)
            .ToList();
        List<Guid> pageIds = [.. matched.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).Select(t => t.Id)];
        IReadOnlyList<SchoolRow> rows = await this.RowsAsync(pageIds, cancellationToken).ConfigureAwait(false);
        return new SchoolPage([.. pageIds.Select(id => rows.Single(r => r.TenantId == id))], matched.Count, filter.Page, filter.PageSize);
    }

    public async Task<SchoolRow?> FindAsync(Guid tenantId, CancellationToken cancellationToken) =>
        (await this.RowsAsync([tenantId], cancellationToken).ConfigureAwait(false)).SingleOrDefault();

    public Task<Tenant?> FindTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        this.context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

    private async Task<IReadOnlyList<SchoolRow>> RowsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var tenants = await this.context.Tenants.IgnoreQueryFilters().Where(t => ids.Contains(t.Id))
            .Select(t => new { t.Id, t.Name, t.CreatedAt, t.IsActive }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var branches = await this.context.Branches.IgnoreQueryFilters().Where(b => ids.Contains(b.TenantId) && b.IsActive)
            .GroupBy(b => b.TenantId).Select(g => new { TenantId = g.Key, Count = g.Count() }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var members = await this.context.Memberships.IgnoreQueryFilters().Where(m => ids.Contains(m.TenantId) && m.IsActive)
            .GroupBy(m => m.TenantId).Select(g => new { TenantId = g.Key, Count = g.Count() }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var founders = await (
            from r in this.context.RoleAssignments.IgnoreQueryFilters()
            join u in this.context.Users on r.UserId equals u.Id
            where ids.Contains(r.TenantId) && r.Role == Role.Founder
                && this.context.Memberships.IgnoreQueryFilters().Any(m => m.UserId == r.UserId && m.TenantId == r.TenantId && m.IsActive)
            select new { r.TenantId, u.FullName, u.Email })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. tenants.Select(t => new SchoolRow(
            t.Id,
            t.Name,
            t.CreatedAt,
            t.IsActive,
            branches.FirstOrDefault(b => b.TenantId == t.Id)?.Count ?? 0,
            members.FirstOrDefault(m => m.TenantId == t.Id)?.Count ?? 0,
            [.. founders.Where(f => f.TenantId == t.Id).Select(f => new SchoolFounder(f.FullName, f.Email)).Distinct().OrderBy(f => f.FullName, TurkishText.IgnoreCase)]))];
    }
}
