namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>Talep, yanıt ya da iptalin sonucu (spec 0016): randevu ve yeni durumu (<c>Pending</c>, <c>Approved</c>, <c>Rejected</c>, <c>CancelledByGuardian</c>, <c>CancelledByTeacher</c>).</summary>
public sealed record AppointmentChanged(Guid AppointmentId, string State);
