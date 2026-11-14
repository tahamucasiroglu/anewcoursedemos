namespace TahaKonsept.Modules.Providers.Tests.Support;

/// <summary>
/// Yapılandırılmış çıktı testleri için örnek tür.
/// </summary>
public sealed record SampleConcept
{
    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Sahne sayısı.</summary>
    public int SceneCount { get; init; }

    /// <summary>Etiketler.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Opsiyonel not.</summary>
    public string? Note { get; init; }
}
