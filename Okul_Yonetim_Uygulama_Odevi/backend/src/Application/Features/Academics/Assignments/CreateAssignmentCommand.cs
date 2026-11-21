using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>
/// Sınıfa ödev verir (spec 0013 gereksinim 1): ders adı, başlık, isteğe bağlı açıklama ve teslim günü. Ödev metni taşıdığı için
/// bütünüyle loglanmaz (T9); işleyici yalnızca kimlik ve sayıları loglar.
/// </summary>
public sealed record CreateAssignmentCommand(Guid ClassroomId, string? Subject, string? Title, string? Description, DateOnly? DueDate) : IRequest<AssignmentCreated>;
