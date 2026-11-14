namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konseptin dayandığı kaynak (başlık + URL); gerçek bilgi doğrulaması için.
/// </summary>
public sealed record ConceptSource
{
    /// <summary>Kaynak başlığı.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Kaynak adresi.</summary>
    public string Url { get; init; } = string.Empty;
}
