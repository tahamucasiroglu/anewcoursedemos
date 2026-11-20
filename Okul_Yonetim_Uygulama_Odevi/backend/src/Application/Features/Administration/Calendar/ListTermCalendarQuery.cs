using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Calendar;

/// <summary>Yönetim ekranı için bir dönemin takvim kayıtları, başlangıç gününe göre sıralı.</summary>
public sealed record ListTermCalendarQuery(Guid TermId) : IRequest<IReadOnlyList<CalendarEntryView>>, ILoggableRequest;
