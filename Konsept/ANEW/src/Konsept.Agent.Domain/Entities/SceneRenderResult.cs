using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Domain.Entities;

/// <summary>Sahne render sonucu ve kısa ayrıntı (iç yol, stack trace yok; düzeltme istemine veri olarak gider).</summary>
/// <param name="Outcome">Sonuç.</param>
/// <param name="Detail">Kısa ayrıntı ya da <c>null</c>.</param>
public sealed record SceneRenderResult(SceneRenderOutcome Outcome, string? Detail = null);
