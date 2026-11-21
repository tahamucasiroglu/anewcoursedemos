namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// <see cref="WeeklySummaryGuard"/>'tan geçmiş istek (spec 0014 gereksinim 6): seçili okul, öğretmen, öğrenci (ad-soyad — modele
/// yalnızca adı gider) ve öğrencinin bugünkü sınıfı (öğretmen ona atanmış). Yalnızca guard üretir.
/// </summary>
public sealed record WeeklySummaryStudent(Guid TenantId, Guid TeacherUserId, Guid StudentId, string FullName, Guid ClassroomId);
