using System.Globalization;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Students.Queries.ListClassroomRoster;
using OkulYonetim.Application.Features.Students.Queries.ListGuardianContacts;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Diğer modüllerin öğrenci okumaları (spec 0010): yoklama listesi (Türkçe ad sırası, aynı adda kimlik sırası) ve bildirim
/// için okulda etkin veliler. Uç yoktur; yetki denetimi çağıran modüldedir.
/// </summary>
public sealed class StudentLookupHandlers :
    IRequestHandler<ListClassroomRosterQuery, IReadOnlyList<RosterStudent>>,
    IRequestHandler<ListGuardianContactsQuery, IReadOnlyList<GuardianContact>>
{
    private static readonly StringComparer TurkishOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    private readonly IStudentRepository students;
    private readonly IStudentGuardianRepository guardians;

    public StudentLookupHandlers(IStudentRepository students, IStudentGuardianRepository guardians)
    {
        this.students = students;
        this.guardians = guardians;
    }

    public async Task<IReadOnlyList<RosterStudent>> Handle(ListClassroomRosterQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return [.. (await this.students.ListClassroomRosterAsync(request.ClassroomId, cancellationToken).ConfigureAwait(false))
            .OrderBy(s => s.FullName, TurkishOrder).ThenBy(s => s.Id)];
    }

    public async Task<IReadOnlyList<GuardianContact>> Handle(ListGuardianContactsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.StudentIds.Count == 0
            ? []
            : await this.guardians.ListActiveGuardianContactsAsync(request.StudentIds, cancellationToken).ConfigureAwait(false);
    }
}
