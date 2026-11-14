namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>
/// <c>Mux</c> aşamasının ajan girdisi: birleşik videoyu son konumuna (<c>Videos</c> klasörü) taşı, küçük resmi hazırla,
/// yerel video dizinine kaydet. Video kimliği <c>StageCommand.VideoId</c>'dir.
/// </summary>
public sealed record MuxStagePayload
{
    /// <summary>Video başlığı (dosya adında kullanılır).</summary>
    public string Title { get; init; } = string.Empty;
}
