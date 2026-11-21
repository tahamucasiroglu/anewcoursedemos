using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Organization.Queries.GetTermById;

/// <summary>Dönemi kurum filtresinden okur; bulunamazsa 404 — başka okulda var olup olmadığı belli edilmez (S1).</summary>
public sealed class GetTermByIdQueryHandler : IRequestHandler<GetTermByIdQuery, TermSummary>
{
    private static readonly string[] Locales = ["tr"];
    private readonly ITermRepository terms;
    private readonly ILocalizationService localization;

    public GetTermByIdQueryHandler(ITermRepository terms, ILocalizationService localization)
    {
        this.terms = terms;
        this.localization = localization;
    }

    public async Task<TermSummary> Handle(GetTermByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Term? term = await this.terms.FindAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(await this.localization.GetLocalizedAsync("TermNotFound", Locales, "Organization").ConfigureAwait(false));
        return new TermSummary(term.Id, term.Name, term.StartDate, term.EndDate);
    }
}
