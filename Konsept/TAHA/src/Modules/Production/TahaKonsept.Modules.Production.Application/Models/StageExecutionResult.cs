using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Sunucu aşamasının (ya da ajan sonucunun işlenmesinin) sonucu: çıktı JSON'u, artefaktlar, maliyet, token'lar, mesaj.
/// </summary>
public sealed record StageExecutionResult
{
    /// <summary>Çıktı JSON'u.</summary>
    public string? OutputJson { get; init; }

    /// <summary>Online üretilen artefaktlar.</summary>
    public IReadOnlyList<ArtifactDownload> Artifacts { get; init; } = [];

    /// <summary>Maliyet ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Giriş token'ı.</summary>
    public long InputTokens { get; init; }

    /// <summary>Çıkış token'ı.</summary>
    public long OutputTokens { get; init; }

    /// <summary>Mesaj.</summary>
    public string? Message { get; init; }
}
