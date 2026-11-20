using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Calendar;

/// <summary>
/// Dönemin takvimine kayıt ekle (spec 0008 gereksinim 7): başlık (≤ 100), tür (<c>Holiday</c>/<c>Exam</c>/<c>Event</c>),
/// başlangıç ve bitiş günü zorunlu; iki gün de dönemin içinde. Kişisel veri taşımaz.
/// </summary>
public sealed record CreateCalendarEntryCommand(Guid TermId, string Title, string? Kind, DateOnly? StartDate, DateOnly? EndDate) : IRequest<CalendarEntryView>, ILoggableRequest;
