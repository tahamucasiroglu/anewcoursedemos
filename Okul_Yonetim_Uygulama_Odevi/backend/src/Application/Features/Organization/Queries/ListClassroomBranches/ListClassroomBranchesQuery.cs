using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Organization.Queries.ListClassroomBranches;

/// <summary>
/// Verilen sınıfların şubeleri (spec 0017: sınıf kapsamlı duyurunun şube süzgeci). Modüller arası okuma; uç yok — yetki çağıranda.
/// Görünmeyen sınıf listede yer almaz; pasif sınıf da döner (eski duyurunun şubesi değişmez).
/// </summary>
public sealed record ListClassroomBranchesQuery(IReadOnlyCollection<Guid> Ids) : IRequest<IReadOnlyList<ClassroomBranch>>;
