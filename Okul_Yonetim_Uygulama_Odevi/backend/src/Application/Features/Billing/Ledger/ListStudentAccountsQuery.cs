using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Planı olan öğrencilerin cari listesi (spec 0015 gereksinim 8): şube, "yalnızca gecikenler", ad araması, 25'lik sayfa.</summary>
public sealed record ListStudentAccountsQuery(Guid? BranchId, bool OverdueOnly, string? Search, int Page = 1) : IRequest<StudentAccountPage>;
