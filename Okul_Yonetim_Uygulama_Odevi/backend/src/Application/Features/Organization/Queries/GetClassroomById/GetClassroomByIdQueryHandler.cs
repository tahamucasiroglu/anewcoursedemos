using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Organization.Queries.GetClassroomById;

/// <summary>Sınıfı filtreli okur; bulunamazsa 404 — başka okulda ya da yetkisiz/pasif şubede var olup olmadığı belli edilmez (S1).</summary>
public sealed class GetClassroomByIdQueryHandler : IRequestHandler<GetClassroomByIdQuery, ClassroomSummary>
{
    private static readonly string[] Locales = ["tr"];
    private readonly IClassroomRepository classrooms;
    private readonly IBranchRepository branches;
    private readonly IGradeLevelRepository gradeLevels;
    private readonly ILocalizationService localization;

    public GetClassroomByIdQueryHandler(IClassroomRepository classrooms, IBranchRepository branches, IGradeLevelRepository gradeLevels, ILocalizationService localization)
    {
        this.classrooms = classrooms;
        this.branches = branches;
        this.gradeLevels = gradeLevels;
        this.localization = localization;
    }

    public async Task<ClassroomSummary> Handle(GetClassroomByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Classroom? classroom = await this.classrooms.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        Branch? branch = classroom is null ? null : await this.branches.FindVisibleAsync(classroom.BranchId, cancellationToken).ConfigureAwait(false);
        GradeLevel? level = classroom is null ? null : await this.gradeLevels.FindAsync(classroom.GradeLevelId, cancellationToken).ConfigureAwait(false);
        if (classroom is null || branch is null || level is null)
        {
            throw new NotFoundException(await this.localization.GetLocalizedAsync("ClassroomNotFound", Locales, "Organization").ConfigureAwait(false));
        }

        return new ClassroomSummary(classroom.Id, classroom.TermId, branch.Id, branch.Name, level.Id, level.Name, classroom.Name, classroom.IsActive);
    }
}
