namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Onaylanan konseptleri üretim sırasına ekleme isteği (<c>POST api/production/queue</c>).
/// </summary>
public sealed record AddToQueueRequest
{
    /// <summary>Sıraya eklenecek konseptler (verilen sırayla eklenir).</summary>
    public IReadOnlyList<Guid> ConceptIds { get; init; } = [];

    /// <summary>Bu öğeler için ayar (verilmezse iş başlarken kullanıcının güncel ayarları kullanılır).</summary>
    public ProductionSettingsDto? Settings { get; init; }
}
