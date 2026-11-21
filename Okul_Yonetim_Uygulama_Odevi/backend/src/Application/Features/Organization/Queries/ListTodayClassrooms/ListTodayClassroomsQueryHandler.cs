using System.Globalization;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Organization.Queries.ListTodayClassrooms;

/// <summary>
/// Bugünü <see cref="SchoolCalendar"/> ile (Türkiye günü) bulur; bugünü kapsayan dönem yoksa ya da takvimde tatilse okul günü değildir
/// (yaklaşan dönem okul günü sayılmaz). Okul günüyse dönemin etkin sınıflarından görünür ve etkin şubelerdekileri şube adı, seviye sırası
/// ve sınıf adıyla (Türkçe) sıralar; atanmış öğretmen adları tek sorguyla eklenir (T7).
/// </summary>
public sealed class ListTodayClassroomsQueryHandler : IRequestHandler<ListTodayClassroomsQuery, TodayClassrooms>
{
    private static readonly StringComparer Turkish = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);
    private readonly ITermRepository terms;
    private readonly ICalendarEntryRepository entries;
    private readonly IClassroomRepository classrooms;
    private readonly IBranchRepository branches;
    private readonly IGradeLevelRepository gradeLevels;
    private readonly IClassroomTeacherRepository teachers;
    private readonly SchoolCalendar calendar;

    public ListTodayClassroomsQueryHandler(
        ITermRepository terms,
        ICalendarEntryRepository entries,
        IClassroomRepository classrooms,
        IBranchRepository branches,
        IGradeLevelRepository gradeLevels,
        IClassroomTeacherRepository teachers,
        SchoolCalendar calendar)
    {
        this.terms = terms;
        this.entries = entries;
        this.classrooms = classrooms;
        this.branches = branches;
        this.gradeLevels = gradeLevels;
        this.teachers = teachers;
        this.calendar = calendar;
    }

    public async Task<TodayClassrooms> Handle(ListTodayClassroomsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateOnly today = this.calendar.Today();
        Term? term = (await this.terms.ListAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(t => t.Contains(today));
        if (term is null || !SchoolCalendar.IsSchoolDay(term, await this.entries.ListByTermAsync(term.Id, cancellationToken).ConfigureAwait(false), today))
        {
            return new TodayClassrooms(today, false, []);
        }

        Dictionary<Guid, Branch> branchById = (await this.branches.ListVisibleAsync(cancellationToken).ConfigureAwait(false))
            .Where(b => b.IsActive && (request.BranchId is null || b.Id == request.BranchId))
            .ToDictionary(b => b.Id);
        Dictionary<Guid, GradeLevel> levelById = (await this.gradeLevels.ListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(g => g.Id);
        List<Classroom> active = [.. (await this.classrooms.ListByTermAsync(term.Id, cancellationToken).ConfigureAwait(false))
            .Where(c => c.IsActive && branchById.ContainsKey(c.BranchId))];
        ILookup<Guid, string> names = (active.Count == 0 ? [] : await this.teachers.ListAssignedNamesAsync([.. active.Select(c => c.Id)], cancellationToken).ConfigureAwait(false))
            .ToLookup(t => t.ClassroomId, t => t.FullName);
        return new TodayClassrooms(today, true, [.. active
            .OrderBy(c => branchById[c.BranchId].Name, Turkish)
            .ThenBy(c => levelById.TryGetValue(c.GradeLevelId, out GradeLevel? level) ? level.SortOrder : int.MaxValue)
            .ThenBy(c => c.Name, Turkish)
            .Select(c => new TodayClassroom(c.Id, c.BranchId, branchById[c.BranchId].Name, c.Name, [.. names[c.Id].Order(Turkish)]))]);
    }
}
