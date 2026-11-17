using Konsept.Modules.Concepts.Contracts.Dtos;
using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>
/// Sürüm işleminin bağlamı (spec 0020; servis içi): kullanıcı, videonun sürümleri, taban sürüm ve ayarları, güncel konsept metni, varsa videonun
/// tamamlanmamış işi.
/// </summary>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="Rows">Sürümler (eskiden yeniye).</param>
/// <param name="Base">Taban sürüm.</param>
/// <param name="Settings">Taban sürümün sabit ayarları.</param>
/// <param name="Content">Güncel konsept metni.</param>
/// <param name="Active">Videonun tamamlanmamış işi ya da <c>null</c>.</param>
public sealed record VersionContext(
    Guid UserId, IReadOnlyList<ProducedVideo> Rows, ProducedVideo Base, FixedJobSettings Settings, ConceptContent Content, ProductionJob? Active);
