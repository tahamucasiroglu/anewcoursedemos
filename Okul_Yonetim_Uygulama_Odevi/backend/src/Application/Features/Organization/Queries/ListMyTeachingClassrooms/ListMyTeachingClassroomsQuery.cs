using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Organization.Queries.ListMyTeachingClassrooms;

/// <summary>
/// Oturumdaki kişinin öğretmen olarak atandığı, görünür ve etkin sınıfları (spec 0014: haftalık özet sınıf listesi ve "Haftalık
/// özet" bağlantısı): kişinin Öğretmen rolü olduğu şubelerdeki sınıflar, dönemi verilen günde ya da sonra biten. Öğretmen değilse boş
/// liste (403 vermez — çağıran karar verir). Kural <see cref="TeacherClassroomAccess"/> ile aynı kaynaktan. Yalnızca kimlik taşır.
/// </summary>
public sealed record ListMyTeachingClassroomsQuery(DateOnly TermEndsOnOrAfter) : IRequest<IReadOnlyList<TeacherClassroomRow>>;
