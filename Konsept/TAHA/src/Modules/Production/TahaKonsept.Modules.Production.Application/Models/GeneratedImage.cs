namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Üretilen (ya da üretilecek) tek görsel: sahne, dosya adı, istem ve online üretildiyse artefakt kimliği.
/// </summary>
public sealed record GeneratedImage
{
    /// <summary>Sahne.</summary>
    public int SceneIndex { get; init; }

    /// <summary>Dosya adı.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>İstem.</summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>Online artefakt (yerel üretimde <c>null</c>).</summary>
    public Guid? ArtifactId { get; init; }
}
