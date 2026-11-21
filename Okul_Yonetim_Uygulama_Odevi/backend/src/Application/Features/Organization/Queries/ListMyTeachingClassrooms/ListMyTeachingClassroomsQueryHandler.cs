using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Organization.Queries.ListMyTeachingClassrooms;

/// <summary>Öğretmenin atandığı etkin sınıflar (spec 0014; <see cref="ListMyTeachingClassroomsQuery"/>): rolü olduğu şubelerde, dönemi verilen günden önce bitmemiş.</summary>
public sealed class ListMyTeachingClassroomsQueryHandler : IRequestHandler<ListMyTeachingClassroomsQuery, IReadOnlyList<TeacherClassroomRow>>
{
    private readonly TeacherClassroomAccess access;
    private readonly IClassroomTeacherRepository teachers;

    public ListMyTeachingClassroomsQueryHandler(TeacherClassroomAccess access, IClassroomTeacherRepository teachers)
    {
        this.access = access;
        this.teachers = teachers;
    }

    public async Task<IReadOnlyList<TeacherClassroomRow>> Handle(ListMyTeachingClassroomsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await this.access.TeacherAsync(cancellationToken).ConfigureAwait(false) is not { } teacher)
        {
            return [];
        }

        return [.. (await this.teachers.ListForTeacherAsync(teacher.UserId, cancellationToken).ConfigureAwait(false))
            .Where(c => c.IsActive && teacher.TeacherBranchIds.Contains(c.BranchId) && c.TermEndDate >= request.TermEndsOnOrAfter)];
    }
}
