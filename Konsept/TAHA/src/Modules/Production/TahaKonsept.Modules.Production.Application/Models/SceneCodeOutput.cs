namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// <c>SceneCode</c> aşamasının sahne başına çıktısı: motor ve LLM'in yazdığı sahne kodu.
/// </summary>
public sealed record SceneCodeOutput
{
    /// <summary>Sahne.</summary>
    public int SceneIndex { get; init; }

    /// <summary>Video motoru.</summary>
    public string Engine { get; init; } = string.Empty;

    /// <summary>Sahne kodu.</summary>
    public string Code { get; init; } = string.Empty;
}
