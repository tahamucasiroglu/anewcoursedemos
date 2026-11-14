namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>Tek görsel isteği: sahne, dosya adı, istem ve tohum.</summary>
public sealed record ImagePromptItem
{
    /// <summary>Sahne indeksi.</summary>
    public int SceneIndex { get; init; }

    /// <summary>Dosya adı (ör. <c>scene-2-1.png</c>); sahne kodu görsele bu adla başvurur.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Pozitif istem (görselde yazı istenmez).</summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>Tohum (tekrarlanabilirlik).</summary>
    public long Seed { get; init; }
}
