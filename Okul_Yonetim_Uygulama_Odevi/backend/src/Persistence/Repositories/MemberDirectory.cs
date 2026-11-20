using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Seçili okulun kişileri (spec 0005): üyelik (kurum filtreli) + kişi + bu okuldaki roller (kurum filtreli). Filtre dışı
/// okuma yok; başka okuldaki rol ve üyelikler hiçbir satıra girmez. Rolü kalmayan ya da pasif üyelikli kişi pasiftir.
/// </summary>
public sealed class MemberDirectory : IMemberDirectory
{
    private readonly OkulDbContext context;

    public MemberDirectory(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<MemberPage> ListAsync(MemberListFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var query = from m in this.context.Memberships
                    join u in this.context.Users on m.UserId equals u.Id
                    select new { Membership = m, User = u };
        if (filter.Role is { } role)
        {
            query = query.Where(x => this.context.RoleAssignments.Any(r => r.UserId == x.User.Id && r.Role == role));
        }

        if (filter.BranchId is { } branchId)
        {
            query = query.Where(x => this.context.RoleAssignments.Any(r => r.UserId == x.User.Id && r.BranchId == branchId));
        }

        if (filter.Active is { } active)
        {
            query = active
                ? query.Where(x => x.Membership.IsActive && this.context.RoleAssignments.Any(r => r.UserId == x.User.Id))
                : query.Where(x => !x.Membership.IsActive || !this.context.RoleAssignments.Any(r => r.UserId == x.User.Id));
        }

        // Arama ve sıralama Türkçe harf kuralıyla uygulamada yapılır (veritabanı yerel ayarı "Ş/ş", "İ/i" eşlemeyebilir);
        // okul başına kişi sayısı birkaç yüzle sınırlı olduğundan süzülmüş liste hafiftir.
        var candidates = await query.Select(x => new { x.User.Id, x.User.FullName, x.User.Email, x.User.Phone, x.Membership.IsActive })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var matched = candidates
            .Where(x => filter.Search is not { } search
                || TurkishText.Contains(x.FullName, search)
                || x.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.FullName, TurkishText.IgnoreCase).ThenBy(x => x.Email, StringComparer.Ordinal)
            .ToList();
        int total = matched.Count;
        var page = matched.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();
        Dictionary<Guid, IReadOnlyList<MemberRoleView>> roles = await this.RolesAsync([.. page.Select(p => p.Id)], cancellationToken).ConfigureAwait(false);
        List<MemberRow> rows = [.. page.Select(p => Row(p.Id, p.FullName, p.Email, p.Phone, p.IsActive, roles))];
        return new MemberPage(rows, total, filter.Page, filter.PageSize);
    }

    public async Task<MemberRow?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var found = await (from m in this.context.Memberships
                           join u in this.context.Users on m.UserId equals u.Id
                           where u.Id == userId
                           select new { u.Id, u.FullName, u.Email, u.Phone, m.IsActive })
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (found is null)
        {
            return null;
        }

        Dictionary<Guid, IReadOnlyList<MemberRoleView>> roles = await this.RolesAsync([userId], cancellationToken).ConfigureAwait(false);
        return Row(found.Id, found.FullName, found.Email, found.Phone, found.IsActive, roles);
    }

    private static MemberRow Row(Guid id, string fullName, string email, string? phone, bool membershipActive, Dictionary<Guid, IReadOnlyList<MemberRoleView>> roles)
    {
        IReadOnlyList<MemberRoleView> mine = roles.TryGetValue(id, out IReadOnlyList<MemberRoleView>? list) ? list : [];
        return new MemberRow(id, fullName, email, phone, membershipActive && mine.Count > 0, mine);
    }

    private async Task<Dictionary<Guid, IReadOnlyList<MemberRoleView>>> RolesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        List<RoleAssignment> assignments = await this.context.RoleAssignments.Where(r => userIds.Contains(r.UserId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return assignments.GroupBy(a => a.UserId).ToDictionary(
            g => g.Key,
            g => (IReadOnlyList<MemberRoleView>)[.. g.GroupBy(a => a.Role).OrderBy(r => r.Key)
                .Select(r => new MemberRoleView(r.Key.ToString(), [.. r.Where(a => a.BranchId is not null).Select(a => a.BranchId!.Value)]))]);
    }
}
