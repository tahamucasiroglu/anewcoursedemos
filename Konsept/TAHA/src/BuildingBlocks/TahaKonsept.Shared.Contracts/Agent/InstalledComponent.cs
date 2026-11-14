namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajanda kurulu bir bileşen (motor, model ya da ön gereksinim) ve hazır olup olmadığı.
/// </summary>
public sealed record InstalledComponent
{
    /// <summary>Bileşen kimliği (katalogdaki id, ör. <c>supertonic-3</c>, <c>ffmpeg</c>).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Bileşen türü.</summary>
    public ComponentKind Kind { get; init; }

    /// <summary>Kurulu sürüm.</summary>
    public string? Version { get; init; }

    /// <summary>Kullanıma hazır mı.</summary>
    public bool IsReady { get; init; }

    /// <summary>Durum notu (ör. hata mesajı).</summary>
    public string? Note { get; init; }
}
