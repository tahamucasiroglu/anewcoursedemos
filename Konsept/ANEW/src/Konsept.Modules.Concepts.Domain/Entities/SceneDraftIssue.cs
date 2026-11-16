namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>Elle düzenlenmiş sahnede geçersiz alan (spec 0015 Req 4).</summary>
/// <param name="Index">Sahne sırası (0'dan).</param>
/// <param name="Field">Alan: <c>title</c> · <c>visual</c> · <c>narration</c> · <c>durationSeconds</c>.</param>
public sealed record SceneDraftIssue(int Index, string Field);
