using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Organization.Queries.GetBranches;

/// <summary>Görünür ve etkin şubeleri ada göre sıralı döndürür (pasif şube listede yok — spec 0005); filtre kurum ve şube sınırını uygular (S1).</summary>
public sealed class GetBranchesQueryHandler : IRequestHandler<GetBranchesQuery, IReadOnlyList<BranchSummary>>
{
    private readonly IBranchRepository branches;

    public GetBranchesQueryHandler(IBranchRepository branches)
    {
        this.branches = branches;
    }

    public async Task<IReadOnlyList<BranchSummary>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
    {
        var visible = await this.branches.ListVisibleAsync(cancellationToken).ConfigureAwait(false);
        return [.. visible
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true))
            .Select(b => new BranchSummary(b.Id, b.Name))];
    }
}
