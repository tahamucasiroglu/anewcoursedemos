using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Organization.Queries.ListClassrooms;

/// <summary>
/// Görünür etkin sınıfları şube adı, seviye sırası ve sınıf adıyla döndürür. Sınıf filtresi şube görünürlüğünü miras
/// alır (S1): kurucu olmayana yalnızca atandığı etkin şubelerin sınıfları gelir. Görünmeyen dönem "bulunamadı".
/// </summary>
public sealed class ListClassroomsQueryHandler : IRequestHandler<ListClassroomsQuery, IReadOnlyList<ClassroomSummary>>
{
    private static readonly string[] Locales = ["tr"];
    private static readonly StringComparer Turkish = StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);
    private readonly IClassroomRepository classrooms;
    private readonly ITermRepository terms;
    private readonly IBranchRepository branches;
    private readonly IGradeLevelRepository gradeLevels;
    private readonly SchoolCalendar calendar;
    private readonly ILocalizationService localization;

    public ListClassroomsQueryHandler(
        IClassroomRepository classrooms,
        ITermRepository terms,
        IBranchRepository branches,
        IGradeLevelRepository gradeLevels,
        SchoolCalendar calendar,
        ILocalizationService localization)
    {
        this.classrooms = classrooms;
        this.terms = terms;
        this.branches = branches;
        this.gradeLevels = gradeLevels;
        this.calendar = calendar;
        this.localization = localization;
    }

    public async Task<IReadOnlyList<ClassroomSummary>> Handle(ListClassroomsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Term? term = request.TermId is { } termId
            ? await this.terms.FindAsync(termId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(await this.localization.GetLocalizedAsync("TermNotFound", Locales, "Organization").ConfigureAwait(false))
            : SchoolCalendar.PickDefault(await this.terms.ListAsync(cancellationToken).ConfigureAwait(false), this.calendar.Today());
        if (term is null)
        {
            return [];
        }

        IReadOnlyList<Classroom> inTerm = await this.classrooms.ListByTermAsync(term.Id, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, Branch> branchById = (await this.branches.ListVisibleAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(b => b.Id);
        Dictionary<Guid, GradeLevel> levelById = (await this.gradeLevels.ListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(g => g.Id);
        return [.. inTerm
            .Where(c => c.IsActive && branchById.ContainsKey(c.BranchId))
            .Select(c => (Classroom: c, Branch: branchById[c.BranchId], Level: levelById[c.GradeLevelId]))
            .OrderBy(x => x.Branch.Name, Turkish)
            .ThenBy(x => x.Level.SortOrder)
            .ThenBy(x => x.Classroom.Name, Turkish)
            .Select(x => new ClassroomSummary(x.Classroom.Id, x.Classroom.TermId, x.Branch.Id, x.Branch.Name, x.Level.Id, x.Level.Name, x.Classroom.Name, x.Classroom.IsActive))];
    }
}
