using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Appointments.Availability;

/// <summary>
/// Öğretmenin müsaitlik açması (spec 0016 gereksinim 1): gün (<c>yyyy-MM-dd</c>, Türkiye günü), başlangıç ve bitiş ("SS:DD") ve görüşme
/// süresi (15/20/30 dakika). Aralık dilimlere bölünür; sonuç açılan dilim sayısı.
/// </summary>
public sealed record OpenAvailabilityCommand(DateOnly? Day, string? Start, string? End, int? DurationMinutes) : IRequest<AvailabilityOpened>;
