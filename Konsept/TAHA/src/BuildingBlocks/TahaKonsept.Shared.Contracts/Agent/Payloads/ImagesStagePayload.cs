namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>
/// <c>Images</c> aşamasının ajan girdisi (<c>StageCommand.PayloadJson</c>): hangi yerel modelle, hangi boyutta, hangi görsellerin üretileceği.
/// Ajan her öğeyi <c>images/&lt;Name&gt;</c> olarak üretir; var olan dosya atlanır (devam/kontrol noktası).
/// </summary>
public sealed record ImagesStagePayload
{
    /// <summary>Model anahtarı (ör. <c>z-image-turbo</c>); bu modeli destekleyen <c>IImageEngine</c> seçilir.</summary>
    public string ModelKey { get; init; } = string.Empty;

    /// <summary>Motor anahtarı (isteğe bağlı; boşsa modeli destekleyen ilk motor).</summary>
    public string? EngineKey { get; init; }

    /// <summary>Genişlik (px).</summary>
    public int Width { get; init; } = 1080;

    /// <summary>Yükseklik (px).</summary>
    public int Height { get; init; } = 1920;

    /// <summary>Adım sayısı (<c>null</c> = model varsayılanı).</summary>
    public int? Steps { get; init; }

    /// <summary>Ortak negatif istem.</summary>
    public string? NegativePrompt { get; init; }

    /// <summary>Üretilecek görseller.</summary>
    public IReadOnlyList<ImagePromptItem> Items { get; init; } = [];
}
