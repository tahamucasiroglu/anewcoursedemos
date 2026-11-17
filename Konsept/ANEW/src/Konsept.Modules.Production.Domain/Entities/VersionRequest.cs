using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>
/// Sürüm işinin isteği (spec 0020): tür, taban sürüm ve onun ayarları, kullanıcının önizlemede gördüğü ve onayladığı çalışacak aşamalar ve
/// online çağrı onayı. Hat yalnızca bu aşamalardaki online çağrıları yapar (H-3); online onayı yoksa render düzeltme çağrısı da yapılmaz.
/// </summary>
public sealed record VersionRequest
{
    /// <summary>Gets tür.</summary>
    public VersionKind Kind { get; init; }

    /// <summary>Gets taban sürüm (video kaydı).</summary>
    public Guid BaseVideoId { get; init; }

    /// <summary>Gets taban sürümün sabit ayarları.</summary>
    public required FixedJobSettings BaseSettings { get; init; }

    /// <summary>Gets onaylanan (çalışacak) aşamalar.</summary>
    public IReadOnlyList<PipelineStage> Stages { get; init; } = [];

    /// <summary>Gets a value indicating whether online model çağrısı onaylandı.</summary>
    public bool Online { get; init; }
}
