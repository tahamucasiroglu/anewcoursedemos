namespace OkulYonetim.Application.Features.Appointments.Availability;

/// <summary>Müsaitlik açmanın sonucu (spec 0016): açılan görüşme saati sayısı ("6 görüşme saati açıldı").</summary>
public sealed record AvailabilityOpened(int SlotCount);
