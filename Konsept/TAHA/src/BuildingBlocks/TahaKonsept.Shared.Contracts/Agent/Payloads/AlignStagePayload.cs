namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>
/// <c>Align</c> aşamasının ajan girdisi: hizalayıcı, dil, ses dosyası ve segmentler.
/// Ses varsayılan olarak aynı işin <c>speech/speech.wav</c> dosyasıdır; online TTS kullanıldıysa <see cref="AudioArtifactName"/> indirilen artefaktı gösterir.
/// Çıktı: <c>align/captions.json</c> ve <see cref="AlignStageResult"/>.
/// </summary>
public sealed record AlignStagePayload
{
    /// <summary>Hizalayıcı anahtarı (ör. <c>proportional</c>, <c>whisper</c>).</summary>
    public string AlignerKey { get; init; } = "proportional";

    /// <summary>Dil kodu.</summary>
    public string Language { get; init; } = "tr";

    /// <summary>İndirilen ses artefaktının adı (isteğe bağlı).</summary>
    public string? AudioArtifactName { get; init; }

    /// <summary>Segmentler (sıralı).</summary>
    public IReadOnlyList<AlignSegmentItem> Segments { get; init; } = [];
}
