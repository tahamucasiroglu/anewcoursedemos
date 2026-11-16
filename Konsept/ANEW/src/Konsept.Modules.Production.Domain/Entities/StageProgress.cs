using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>Bir aşamanın durumu ve (yerel aşamada) çalıştığı aygıt (spec 0017 Req 6, 14a).</summary>
/// <param name="Stage">Aşama.</param>
/// <param name="State">Durum.</param>
/// <param name="Device">Aygıt (<c>gpu</c> · <c>cpu</c>) ya da <c>null</c>.</param>
public sealed record StageProgress(PipelineStage Stage, StageState State, string? Device = null);
