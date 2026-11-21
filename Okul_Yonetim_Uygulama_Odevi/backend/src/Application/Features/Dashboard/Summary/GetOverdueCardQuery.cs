using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Dashboard.Summary;

/// <summary>Günlük özetin geciken ödemeler kartı (spec 0017 gereksinim 4); <c>BranchId</c> boşsa bütün şubeler. Yalnızca kurucu.</summary>
public sealed record GetOverdueCardQuery(Guid? BranchId) : IRequest<OverdueCard>;
