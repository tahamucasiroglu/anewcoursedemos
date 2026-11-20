namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// Duyuru yayınlama (spec 0011): başlık, metin, kapsam (<c>School</c>, <c>Branch</c>, <c>Classroom</c>, <c>Student</c>) ve kapsam
/// kimliği (okulda boş); "Onay iste" seçiliyse soru ve son gün (<c>yyyy-MM-dd</c>, Türkiye günü).
/// </summary>
public sealed record PublishAnnouncementRequest(string? Title, string? Body, string? Scope, Guid? ScopeId, bool RequestConsent, string? ConsentQuestion, DateOnly? ConsentDeadline);
