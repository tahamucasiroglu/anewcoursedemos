using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Communication.Announcements;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Duyuru deposu (spec 0011): duyuru, alıcı, kapsanan öğrenci ve onay cevabı kurum filtreli; öğrenci adları öğrenci
/// filtresinden (görünür şubeler), kişi adları kişi tablosundan eklenir (filtre dışı okuma yok). Liste ve sonuç okumaları
/// izlemesiz projeksiyon (T7); alıcı ve onay satırları okundu/cevap yazmak için izlenir.
/// </summary>
public sealed class AnnouncementRepository : IAnnouncementRepository
{
    private readonly OkulDbContext context;

    public AnnouncementRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<Announcement?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.Announcements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<string> AuthorNameAsync(Guid authorUserId, CancellationToken cancellationToken) =>
        await this.context.Users.AsNoTracking().Where(u => u.Id == authorUserId).Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;

    public Task<AnnouncementRecipient?> FindRecipientAsync(Guid announcementId, Guid guardianUserId, CancellationToken cancellationToken) =>
        this.context.AnnouncementRecipients.FirstOrDefaultAsync(r => r.AnnouncementId == announcementId && r.GuardianUserId == guardianUserId, cancellationToken);

    public Task<ConsentResponse?> FindConsentAsync(Guid announcementId, Guid studentId, CancellationToken cancellationToken) =>
        this.context.ConsentResponses.FirstOrDefaultAsync(c => c.AnnouncementId == announcementId && c.StudentId == studentId, cancellationToken);

    public Task<bool> CoversStudentAsync(Guid announcementId, Guid studentId, CancellationToken cancellationToken) =>
        this.context.AnnouncementStudents.AnyAsync(s => s.AnnouncementId == announcementId && s.StudentId == studentId, cancellationToken);

    public async Task<IReadOnlyList<StaffAnnouncementRow>> ListForStaffAsync(Guid? authorUserId, int max, CancellationToken cancellationToken)
    {
        IQueryable<Announcement> query = this.context.Announcements.AsNoTracking();
        if (authorUserId is { } author)
        {
            query = query.Where(a => a.AuthorUserId == author);
        }

        var rows = await (from a in query
                          join u in this.context.Users.AsNoTracking() on a.AuthorUserId equals u.Id
                          orderby a.PublishedAt descending, a.Id descending
                          select new
                          {
                              a.Id,
                              a.Title,
                              a.Scope,
                              u.FullName,
                              a.PublishedAt,
                              Consent = a.ConsentQuestion != null,
                              a.ConsentDeadline,
                              Recipients = this.context.AnnouncementRecipients.Count(r => r.AnnouncementId == a.Id),
                              Read = this.context.AnnouncementRecipients.Count(r => r.AnnouncementId == a.Id && r.ReadAt != null),
                          })
            .Take(max)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new StaffAnnouncementRow(r.Id, r.Title, r.Scope.ToString(), r.FullName, r.PublishedAt, r.Consent, r.ConsentDeadline, r.Recipients, r.Read))];
    }

    public async Task<IReadOnlyList<FeedAnnouncementRow>> ListForGuardianAsync(Guid guardianUserId, int max, CancellationToken cancellationToken)
    {
        var rows = await (from r in this.context.AnnouncementRecipients.AsNoTracking()
                          join a in this.context.Announcements.AsNoTracking() on r.AnnouncementId equals a.Id
                          join u in this.context.Users.AsNoTracking() on a.AuthorUserId equals u.Id
                          where r.GuardianUserId == guardianUserId
                          orderby a.PublishedAt descending, a.Id descending
                          select new { a.Id, a.Title, u.FullName, a.Scope, a.PublishedAt, IsRead = r.ReadAt != null, Consent = a.ConsentQuestion != null })
            .Take(max)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new FeedAnnouncementRow(r.Id, r.Title, r.FullName, r.Scope.ToString(), r.PublishedAt, r.IsRead, r.Consent))];
    }

    public async Task<IReadOnlyList<AnnouncementStudent>> ListCoveredAsync(IReadOnlyCollection<Guid> announcementIds, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken) =>
        await this.context.AnnouncementStudents.AsNoTracking()
            .Where(s => announcementIds.Contains(s.AnnouncementId) && studentIds.Contains(s.StudentId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<int> CountUnreadAsync(Guid guardianUserId, CancellationToken cancellationToken) =>
        this.context.AnnouncementRecipients.CountAsync(r => r.GuardianUserId == guardianUserId && r.ReadAt == null, cancellationToken);

    public async Task<IReadOnlyList<AnnouncementReadRow>> ListPublishedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        DateTimeOffset start = since.ToUniversalTime();
        var rows = await (from a in this.context.Announcements.AsNoTracking()
                          join u in this.context.Users.AsNoTracking() on a.AuthorUserId equals u.Id
                          where a.PublishedAt >= start
                          select new
                          {
                              a.Id,
                              a.Title,
                              a.Scope,
                              a.ScopeId,
                              u.FullName,
                              a.PublishedAt,
                              Recipients = this.context.AnnouncementRecipients.Count(r => r.AnnouncementId == a.Id),
                              Read = this.context.AnnouncementRecipients.Count(r => r.AnnouncementId == a.Id && r.ReadAt != null),
                          })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new AnnouncementReadRow(r.Id, r.Title, r.Scope, r.ScopeId, r.FullName, r.PublishedAt, r.Recipients, r.Read))];
    }

    public Task<int> CountUnreadGuardiansAsync(IReadOnlyCollection<Guid> announcementIds, CancellationToken cancellationToken) =>
        this.context.AnnouncementRecipients.AsNoTracking()
            .Where(r => announcementIds.Contains(r.AnnouncementId) && r.ReadAt == null)
            .Select(r => r.GuardianUserId)
            .Distinct()
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<RecipientRow>> ListRecipientsAsync(Guid announcementId, CancellationToken cancellationToken) =>
        await (from r in this.context.AnnouncementRecipients.AsNoTracking()
               join u in this.context.Users.AsNoTracking() on r.GuardianUserId equals u.Id
               where r.AnnouncementId == announcementId
               select new RecipientRow(r.GuardianUserId, u.FullName, r.ReadAt))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RecipientChild>> ListRecipientChildrenAsync(Guid announcementId, CancellationToken cancellationToken) =>
        await (from r in this.context.AnnouncementRecipients.AsNoTracking()
               join link in this.context.StudentGuardians.AsNoTracking() on r.GuardianUserId equals link.GuardianUserId
               join covered in this.context.AnnouncementStudents.AsNoTracking()
                   on new { r.AnnouncementId, link.StudentId } equals new { covered.AnnouncementId, covered.StudentId }
               join s in this.context.Students.AsNoTracking() on link.StudentId equals s.Id
               where r.AnnouncementId == announcementId
               select new RecipientChild(r.GuardianUserId, s.FullName))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ConsentChildView>> ListConsentAsync(Guid announcementId, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken)
    {
        IQueryable<AnnouncementStudent> covered = this.context.AnnouncementStudents.AsNoTracking().Where(s => s.AnnouncementId == announcementId);
        if (studentIds is not null)
        {
            covered = covered.Where(s => studentIds.Contains(s.StudentId));
        }

        var rows = await (from s in covered
                          join c in this.context.ConsentResponses.AsNoTracking()
                              on new { s.AnnouncementId, s.StudentId } equals new { c.AnnouncementId, c.StudentId } into answers
                          from c in answers.DefaultIfEmpty()
                          select new
                          {
                              s.StudentId,
                              Name = this.context.Students.Where(st => st.Id == s.StudentId).Select(st => st.FullName).FirstOrDefault(),
                              Answer = c == null ? (ConsentAnswer?)null : c.Answer,
                              By = c == null ? null : this.context.Users.Where(u => u.Id == c.AnsweredByUserId).Select(u => u.FullName).FirstOrDefault(),
                              At = c == null ? (DateTimeOffset?)null : c.AnsweredAt,
                          })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new ConsentChildView(r.StudentId, r.Name ?? string.Empty, r.Answer?.ToString(), r.By, r.At))];
    }

    public void Add(Announcement announcement) => this.context.Announcements.Add(announcement);

    public void AddRecipient(AnnouncementRecipient recipient) => this.context.AnnouncementRecipients.Add(recipient);

    public void AddStudent(AnnouncementStudent student) => this.context.AnnouncementStudents.Add(student);

    public void AddConsent(ConsentResponse response) => this.context.ConsentResponses.Add(response);
}
