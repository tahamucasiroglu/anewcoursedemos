namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Üretim sırasının tamamı (<c>GET api/production/queue</c>; web 2 sn'de bir sorgular): çalışma durumu, ajan/bütçe bilgisi ve öğeler.
/// </summary>
public sealed record QueueStateDto
{
    /// <summary>Sıranın çalışma durumu.</summary>
    public QueueRunState Status { get; init; }

    /// <summary>Genel mesaj (ör. bütçe aşıldı, ajan bekleniyor).</summary>
    public string? Message { get; init; }

    /// <summary>Masaüstü programı çevrimiçi mi.</summary>
    public bool AgentOnline { get; init; }

    /// <summary>Bütçe aşıldığı için yeni iş başlatılmıyor mu.</summary>
    public bool BudgetBlocked { get; init; }

    /// <summary>Öğeler (sıraya göre).</summary>
    public IReadOnlyList<QueueItemDto> Items { get; init; } = [];
}
