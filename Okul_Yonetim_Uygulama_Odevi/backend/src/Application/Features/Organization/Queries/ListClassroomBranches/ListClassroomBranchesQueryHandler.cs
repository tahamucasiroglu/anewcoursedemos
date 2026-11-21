using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Organization.Queries.ListClassroomBranches;

/// <summary>Sınıfları kimlikleriyle tek sorguda (kurum + şube filtresinden geçerek) okur ve şubelerini döndürür.</summary>
public sealed class ListClassroomBranchesQueryHandler : IRequestHandler<ListClassroomBranchesQuery, IReadOnlyList<ClassroomBranch>>
{
    private readonly IClassroomRepository classrooms;

    public ListClassroomBranchesQueryHandler(IClassroomRepository classrooms)
    {
        this.classrooms = classrooms;
    }

    public async Task<IReadOnlyList<ClassroomBranch>> Handle(ListClassroomBranchesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Ids.Count == 0
            ? []
            : [.. (await this.classrooms.ListByIdsAsync(request.Ids, cancellationToken).ConfigureAwait(false)).Select(c => new ClassroomBranch(c.Id, c.BranchId))];
    }
}
