namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Takvim kaydı ekleme/düzenleme isteği: başlık, tür (<c>Holiday</c>, <c>Exam</c>, <c>Event</c>), başlangıç ve bitiş günü.</summary>
public sealed record CalendarEntryRequest(string Title, string? Kind, DateOnly? StartDate, DateOnly? EndDate);
