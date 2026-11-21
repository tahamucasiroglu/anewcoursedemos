namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Talep yanıtı (spec 0016): <c>Approve</c> ya da <c>Reject</c> ve isteğe bağlı ret nedeni (en fazla 300).</summary>
public sealed record RespondAppointmentRequest(string? Decision, string? Reason);
