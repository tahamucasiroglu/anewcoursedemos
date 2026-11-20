using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Calendar;

/// <summary>Takvim kaydını düzenle (başlık, tür, tarihler); kayıt kendi döneminin içinde kalmalı.</summary>
public sealed record UpdateCalendarEntryCommand(Guid Id, string Title, string? Kind, DateOnly? StartDate, DateOnly? EndDate) : IRequest<CalendarEntryView>, ILoggableRequest;
