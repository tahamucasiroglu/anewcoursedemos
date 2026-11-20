namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Bir öğrencinin okulda etkin velisi ve iletişim bilgisi (spec 0010 bildirimi): yalnızca Communication'ın bildirim
/// komutu kullanır; hiçbir yanıtta dışarı verilmez ve loglanmaz.
/// </summary>
public sealed record GuardianContact(Guid StudentId, Guid UserId, string FullName, string Email);
