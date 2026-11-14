namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Online görsel modeline giden istek (somut görsel modelleri ayrı bir adımda eklenecek). Görsele yazı yazdırılmaz.
/// </summary>
public sealed record ImageRequest
{
    /// <summary>Kullanıcı + amaç.</summary>
    public CallContext Context { get; init; } = new(Guid.Empty, "image");

    /// <summary>Görsel promptu.</summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>Negatif prompt (destekleyen modellerde).</summary>
    public string? NegativePrompt { get; init; }

    /// <summary>Genişlik (piksel).</summary>
    public int Width { get; init; } = 1080;

    /// <summary>Yükseklik (piksel).</summary>
    public int Height { get; init; } = 1920;

    /// <summary>Tohum (tekrarlanabilirlik; null = rastgele).</summary>
    public long? Seed { get; init; }

    /// <summary>Adet.</summary>
    public int Count { get; init; } = 1;
}
