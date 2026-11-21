namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Randevu talebi (spec 0016): seçilen boş dilim, çocuk ve konu (zorunlu, en fazla 300).</summary>
public sealed record AppointmentRequest(Guid? SlotId, Guid? StudentId, string? Topic);
