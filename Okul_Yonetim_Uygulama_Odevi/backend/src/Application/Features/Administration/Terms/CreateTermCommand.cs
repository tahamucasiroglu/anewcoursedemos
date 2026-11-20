using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Terms;

/// <summary>Dönem ekle (spec 0008 gereksinim 4): ad (≤ 100, okulda benzersiz), başlangıç ve bitiş günü zorunlu. Kişisel veri taşımaz.</summary>
public sealed record CreateTermCommand(string Name, DateOnly? StartDate, DateOnly? EndDate) : IRequest<TermAdminView>, ILoggableRequest;
