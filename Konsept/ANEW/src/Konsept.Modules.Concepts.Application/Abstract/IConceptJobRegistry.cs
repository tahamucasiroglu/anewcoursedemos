using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Süren ve son üretim işleri (Api süreci belleğinde; kullanıcı başına şerit başına bir iş — spec 0014 Req 19–20). Kalıcı değildir
/// (backlog B-93).
/// </summary>
public interface IConceptJobRegistry
{
    /// <summary>İşi kaydeder; aynı kullanıcının aynı şeridinde süren iş varsa kaydetmez.</summary>
    /// <param name="job">İş.</param>
    /// <returns>Kaydedildiyse <c>true</c>.</returns>
    bool TryStart(ConceptJob job);

    /// <summary>Kullanıcının şeritteki son işi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="lane">Şerit.</param>
    /// <returns>İş ya da <c>null</c>.</returns>
    ConceptJob? Get(Guid userId, JobLane lane);

    /// <summary>Kullanıcının süren işlerinin profilleri (süren işin profili silinemez; review r1 M-3).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Profil kimlikleri.</returns>
    IReadOnlySet<Guid> RunningProfiles(Guid userId);
}
