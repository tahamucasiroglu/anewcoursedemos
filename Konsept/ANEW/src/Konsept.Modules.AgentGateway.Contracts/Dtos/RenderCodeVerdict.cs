namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Render öncesi yasaklı yapı kapısının kararı (spec 0018 Req 3): geçti mi, geçmediyse kısa neden (düzeltme istemine veri olarak gider).</summary>
/// <param name="Ok">Kod render edilebilir mi.</param>
/// <param name="Reason">Ret nedeni (İngilizce, kısa; ör. <c>forbidden: fetch</c>) ya da <c>null</c>.</param>
public sealed record RenderCodeVerdict(bool Ok, string? Reason = null)
{
    /// <summary>Geçti.</summary>
    public static RenderCodeVerdict Pass { get; } = new(true);

    /// <summary>Ret kararı.</summary>
    /// <param name="reason">Neden.</param>
    /// <returns>Karar.</returns>
    public static RenderCodeVerdict Reject(string reason) => new(false, reason);
}
