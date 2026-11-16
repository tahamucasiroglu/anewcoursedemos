using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>
/// Konsept başına yeniden kullanılan aşama çıktıları (spec 0017 Req 9, 10, 23): konsept metninin özeti (değişince hepsi geçersiz), analiz planı,
/// sahne başına video kodu ve yerel aşamaların hangi ajan cihazında tamamlandığı (başka cihazda yerel aşamalar yeniden çalışır). Görsel, ses ve
/// klipler ajanda kalır; Pi yalnızca kaydı tutar.
/// </summary>
public sealed record StageOutputs
{
    /// <summary>Gets sahibi.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets konsept (birincil anahtar).</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Gets konsept metninin SHA-256 özeti.</summary>
    public required string TextHash { get; init; }

    /// <summary>Gets analiz planı; yoksa <c>null</c>.</summary>
    public IReadOnlyList<ScenePlan>? Plan { get; init; }

    /// <summary>Gets sahne başına video kodu; yoksa <c>null</c>.</summary>
    public IReadOnlyList<string>? Codes { get; init; }

    /// <summary>Gets yerel aşamaların tamamlandığı ajan cihazı.</summary>
    public Guid? LocalDevice { get; init; }

    /// <summary>Gets o cihazda tamamlanan yerel aşamalar.</summary>
    public IReadOnlyList<PipelineStage> LocalDone { get; init; } = [];

    /// <summary>Gets son güncelleme (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
