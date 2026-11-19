using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Organization.Queries.GetBranchById;

/// <summary>Şubeyi filtreli okur; bulunamazsa 404 — kaydın başka okulda var olup olmadığı belli edilmez (S1).</summary>
public sealed class GetBranchByIdQueryHandler : IRequestHandler<GetBranchByIdQuery, BranchSummary>
{
    private static readonly string[] Locales = ["tr"];
    private readonly IBranchRepository branches;
    private readonly ILocalizationService localization;

    public GetBranchByIdQueryHandler(IBranchRepository branches, ILocalizationService localization)
    {
        this.branches = branches;
        this.localization = localization;
    }

    public async Task<BranchSummary> Handle(GetBranchByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Branch? branch = await this.branches.FindVisibleAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (branch is null)
        {
            throw new NotFoundException(await this.localization.GetLocalizedAsync("BranchNotFound", Locales, "Organization").ConfigureAwait(false));
        }

        return new BranchSummary(branch.Id, branch.Name);
    }
}
