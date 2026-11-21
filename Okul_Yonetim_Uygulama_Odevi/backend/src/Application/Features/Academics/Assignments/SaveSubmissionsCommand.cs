using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>
/// Teslim durumlarını tek seferde kaydeder ya da düzeltir (spec 0013 gereksinim 4). Gönderilmeyen öğrencinin durumu değişmez.
/// Öğrenci bilgisi taşıdığı için bütünüyle loglanmaz (T9).
/// </summary>
public sealed record SaveSubmissionsCommand(Guid AssignmentId, IReadOnlyList<SubmissionInput>? Entries) : IRequest<SubmissionsSaveResult>;
