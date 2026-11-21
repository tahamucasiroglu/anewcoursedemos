using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization.Queries.ListClassroomBranches;
using OkulYonetim.Application.Features.Students.Queries.ListStudentLabels;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>
/// Okunmayan duyuruları toplar (spec 0017): sayılar 0011 sonuç ekranının sayımıyla aynı (alıcı satırı, okundu = <c>ReadAt</c> dolu). Şube
/// süzgecinde duyurunun şubesi kapsamdan çözülür — şube kapsamında kapsam kimliği, sınıf kapsamında sınıfın şubesi (Organization
/// <see cref="ListClassroomBranchesQuery"/>), öğrenci kapsamında öğrencinin şubesi (Students <see cref="ListStudentLabelsQuery"/>); okul
/// geneli her süzgeçte. Sıra: okumayan sayısı azalan, sonra yeni yayın önce.
/// </summary>
public sealed class UnreadAnnouncementsHandler : IRequestHandler<ListUnreadAnnouncementsQuery, UnreadAnnouncements>
{
    private readonly IAnnouncementRepository announcements;
    private readonly ISender sender;

    public UnreadAnnouncementsHandler(IAnnouncementRepository announcements, ISender sender)
    {
        this.announcements = announcements;
        this.sender = sender;
    }

    public async Task<UnreadAnnouncements> Handle(ListUnreadAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        List<AnnouncementReadRow> rows = [.. (await this.announcements.ListPublishedSinceAsync(request.Since, cancellationToken).ConfigureAwait(false))
            .Where(r => r.ReadCount < r.RecipientCount)];
        if (request.BranchId is { } branchId && rows.Count > 0)
        {
            HashSet<Guid> inBranch = await this.InBranchAsync(rows, branchId, cancellationToken).ConfigureAwait(false);
            rows = [.. rows.Where(r => r.Scope == AnnouncementScope.School || inBranch.Contains(r.Id))];
        }

        if (rows.Count == 0)
        {
            return new UnreadAnnouncements([], 0);
        }

        int guardians = await this.announcements.CountUnreadGuardiansAsync([.. rows.Select(r => r.Id)], cancellationToken).ConfigureAwait(false);
        return new UnreadAnnouncements(
            [.. rows
                .OrderByDescending(r => r.RecipientCount - r.ReadCount)
                .ThenByDescending(r => r.PublishedAt)
                .ThenBy(r => r.Id)
                .Select(r => new UnreadAnnouncementRow(r.Id, r.Title, r.PublishedAt, r.AuthorName, r.Scope.ToString(), r.RecipientCount, r.ReadCount))],
            guardians);
    }

    /// <summary>Şube, sınıf ya da öğrenci kapsamlı duyurulardan şubesi verilen şube olanlar.</summary>
    private async Task<HashSet<Guid>> InBranchAsync(List<AnnouncementReadRow> rows, Guid branchId, CancellationToken cancellationToken)
    {
        List<Guid> classroomIds = [.. rows.Where(r => r.Scope == AnnouncementScope.Classroom && r.ScopeId is not null).Select(r => r.ScopeId!.Value).Distinct()];
        List<Guid> studentIds = [.. rows.Where(r => r.Scope == AnnouncementScope.Student && r.ScopeId is not null).Select(r => r.ScopeId!.Value).Distinct()];
        Dictionary<Guid, Guid> classroomBranch = classroomIds.Count == 0
            ? []
            : (await this.sender.Send(new ListClassroomBranchesQuery(classroomIds), cancellationToken).ConfigureAwait(false)).ToDictionary(c => c.ClassroomId, c => c.BranchId);
        Dictionary<Guid, Guid> studentBranch = studentIds.Count == 0
            ? []
            : (await this.sender.Send(new ListStudentLabelsQuery(studentIds), cancellationToken).ConfigureAwait(false)).ToDictionary(s => s.Id, s => s.BranchId);
        return [.. rows.Where(r => r.ScopeId is { } scopeId && r.Scope switch
        {
            AnnouncementScope.Branch => scopeId == branchId,
            AnnouncementScope.Classroom => classroomBranch.TryGetValue(scopeId, out Guid c) && c == branchId,
            AnnouncementScope.Student => studentBranch.TryGetValue(scopeId, out Guid s) && s == branchId,
            _ => false,
        }).Select(r => r.Id)];
    }
}
