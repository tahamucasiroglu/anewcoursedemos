namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>Velinin seçebileceği boş görüşme saati (spec 0016 gereksinim 3): dilim, başlangıç ve bitiş (UTC).</summary>
public sealed record OpenSlot(Guid SlotId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
