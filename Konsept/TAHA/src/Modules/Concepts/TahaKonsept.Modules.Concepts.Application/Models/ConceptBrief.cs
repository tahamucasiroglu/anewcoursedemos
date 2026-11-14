using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Tek konseptin üretim girdisi: başlık ve/veya fikir, isteğe bağlı kanca ve "aynı fikrin kaçıncı açısı" bilgisi, kaynak türü.
/// </summary>
public sealed record ConceptBrief
{
    /// <summary>Başlık.</summary>
    public string? Title { get; init; }

    /// <summary>Önerilen kanca.</summary>
    public string? Hook { get; init; }

    /// <summary>Kullanıcı fikri.</summary>
    public string? Idea { get; init; }

    /// <summary>Aynı fikirden kaçıncı konsept (1'den; null = tek).</summary>
    public int? AngleNumber { get; init; }

    /// <summary>Kaynak türü.</summary>
    public ConceptOrigin Origin { get; init; } = ConceptOrigin.Topic;

    /// <summary>Görüntülenecek ad (başlık ya da fikrin başı).</summary>
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Title) ? Title!
        : Idea is { Length: > 60 } ? Idea[..60] + "…"
        : Idea ?? string.Empty;
}
