using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Öğrencinin carisi (spec 0015 gereksinim 8).</summary>
public sealed record GetStudentAccountQuery(Guid StudentId) : IRequest<StudentAccount>;
