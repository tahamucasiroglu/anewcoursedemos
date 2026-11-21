namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Randevu iptali (spec 0016): isteğe bağlı neden (en fazla 300; öğretmen iptalinde veliye gider).</summary>
public sealed record CancelAppointmentRequest(string? Reason);
