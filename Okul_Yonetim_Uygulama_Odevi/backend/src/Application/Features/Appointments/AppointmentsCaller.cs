namespace OkulYonetim.Application.Features.Appointments;

/// <summary>Randevu isteğinin sahibi (spec 0016): seçili okul, kişi ve bu okulda Öğretmen / Veli rolü olup olmadığı (veritabanından).</summary>
public sealed record AppointmentsCaller(Guid TenantId, Guid UserId, bool IsTeacher, bool IsGuardian);
