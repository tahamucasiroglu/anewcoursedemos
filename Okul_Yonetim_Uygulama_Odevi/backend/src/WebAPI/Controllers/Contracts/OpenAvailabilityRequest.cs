namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Müsaitlik açma (spec 0016): gün (<c>yyyy-MM-dd</c>, Türkiye günü), başlangıç ve bitiş ("SS:DD", Türkiye saati, 15 dk adım) ve görüşme süresi (15/20/30 dakika).</summary>
public sealed record OpenAvailabilityRequest(DateOnly? Day, string? Start, string? End, int? DurationMinutes);
