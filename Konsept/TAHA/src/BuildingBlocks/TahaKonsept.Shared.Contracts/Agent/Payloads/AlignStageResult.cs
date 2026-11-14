namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary><c>Align</c> aşamasının ajan çıktısı (<c>StageResultReport.OutputJson</c>): kelime zamanlamaları.</summary>
public sealed record AlignStageResult
{
    /// <summary>Kelimeler (yazılış) ve zamanları.</summary>
    public IReadOnlyList<CaptionWordItem> Words { get; init; } = [];
}
