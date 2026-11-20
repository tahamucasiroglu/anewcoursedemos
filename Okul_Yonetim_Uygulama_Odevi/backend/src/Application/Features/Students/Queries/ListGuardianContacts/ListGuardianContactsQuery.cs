using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Queries.ListGuardianContacts;

/// <summary>
/// Öğrencilerin okulda etkin velileri ve iletişim bilgisi (spec 0010 bildirimi). Modüller arası okuma; uç yok — yalnızca
/// Communication'ın bildirim komutu çağırır. Kişisel veri taşıdığı için loglanmaz (T9).
/// </summary>
public sealed record ListGuardianContactsQuery(IReadOnlyCollection<Guid> StudentIds) : IRequest<IReadOnlyList<GuardianContact>>;
