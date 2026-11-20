using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Duyuru yayınlama (spec 0011 gereksinim 1, 4): başlık, metin, kapsam (<c>School</c>/<c>Branch</c>/<c>Classroom</c>/<c>Student</c>) + kapsam kimliği ve isteğe bağlı onay formu (soru + son gün). Metin taşıdığı için loglanmaz (T9).</summary>
public sealed record PublishAnnouncementCommand(string? Title, string? Body, string? Scope, Guid? ScopeId, bool RequestConsent, string? ConsentQuestion, DateOnly? ConsentDeadline) : IRequest<AnnouncementPublished>;
