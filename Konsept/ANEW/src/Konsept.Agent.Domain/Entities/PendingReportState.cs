namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// Ajanda kalıcı tutulan bekleyen bildirim durumu (spec 0022 Req 7–9): bilinen son hesap (bekleyen bildirimi oluştuğu hesaba bağlamak
/// için) ve en fazla 20 bekleyen rapor (eskiden yeniye). Ajan yeniden başlasa da kaybolmaz.
/// </summary>
/// <param name="KnownAccount">Ajanın bildiği eşleşmiş hesap; eşleşmemişse <c>null</c>.</param>
/// <param name="Reports">Bekleyen raporlar.</param>
public sealed record PendingReportState(Guid? KnownAccount, IReadOnlyList<AgentErrorReport> Reports)
{
    /// <summary>Gets boş durum.</summary>
    public static PendingReportState Empty { get; } = new(null, []);
}
