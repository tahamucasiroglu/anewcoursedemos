namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Bir aşama çalıştırma isteği: sahne, hesaplanan girdi hash'i, (sahne kodu için) doğrulama hatası ve deneme numarası.
/// </summary>
public sealed record StageExecutionRequest
{
    /// <summary>Sahne.</summary>
    public int? SceneIndex { get; init; }

    /// <summary>Girdi hash'i.</summary>
    public string InputHash { get; init; } = string.Empty;

    /// <summary>Önceki denemenin doğrulama hatası.</summary>
    public string? RepairHint { get; init; }

    /// <summary>Deneme numarası.</summary>
    public int Attempt { get; init; } = 1;
}
