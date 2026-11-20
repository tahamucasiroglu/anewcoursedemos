using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Queries.ListClassroomRoster;

/// <summary>
/// Sınıfa kayıtlı ve ayrılmamış öğrenciler, Türkçe ad sırasıyla (spec 0010 yoklama listesi). Modüller arası okuma; uç yok —
/// yetki çağıranda (yoklamanın öğretmen kapısı). Öğrenci filtresi yine uygulanır (görünmeyen şubenin öğrencisi gelmez).
/// </summary>
public sealed record ListClassroomRosterQuery(Guid ClassroomId) : IRequest<IReadOnlyList<RosterStudent>>;
