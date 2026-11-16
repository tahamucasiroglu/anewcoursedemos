namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Yerel aşamanın sonucu (spec 0017 Req 11, 12, 14a): başarı, hata kodu (<see cref="ToolMissing"/> · <see cref="GpuUnavailable"/> ·
/// <see cref="RenderFailed"/> · <see cref="CodeRejected"/> · <see cref="RenderLimit"/> · <see cref="OutputInvalid"/> · <see cref="Failed"/> · <see cref="Canceled"/>), kısa ayrıntı (araç/model adı ya da render hatası özeti; iç yol,
/// stack trace yok), hatalı sahne, aşamanın çalıştığı aygıt (<c>gpu</c> · <c>cpu</c>) ve AI ses notunun yazılıp yazılmadığı.
/// </summary>
/// <param name="Ok">Başarılı mı.</param>
/// <param name="Error">Hata kodu ya da <c>null</c>.</param>
/// <param name="Detail">Kısa ayrıntı.</param>
/// <param name="FailedScene">Render'da hatalı sahne.</param>
/// <param name="Device">Aygıt.</param>
/// <param name="AiVoiceNotice">AI ses notu yazıldı mı.</param>
public sealed record AgentStageReply(bool Ok, string? Error = null, string? Detail = null, int? FailedScene = null, string? Device = null, bool AiVoiceNotice = false)
{
    /// <summary>Araç ya da model kurulu değil.</summary>
    public const string ToolMissing = "tool_missing";

    /// <summary>Ekran kartı kullanılamadı (CPU'ya düşülmez, Req 14a).</summary>
    public const string GpuUnavailable = "gpu_unavailable";

    /// <summary>Render hatası (düzeltme döngüsüne girer, Req 12).</summary>
    public const string RenderFailed = "render_failed";

    /// <summary>Video kodu render öncesi yasaklı yapı denetiminden geçmedi (spec 0018 Req 3; düzeltme döngüsüne girer).</summary>
    public const string CodeRejected = "code_rejected";

    /// <summary>Render süre, bellek ya da süreç sınırını aştı; süreç ağacı sonlandırıldı (spec 0018 Req 1).</summary>
    public const string RenderLimit = "render_limit";

    /// <summary>Render çıktısı beklenen biçimde değil (spec 0018 Req 4).</summary>
    public const string OutputInvalid = "output_invalid";

    /// <summary>Diğer hata.</summary>
    public const string Failed = "failed";

    /// <summary>Durdur ile kesildi.</summary>
    public const string Canceled = "canceled";
}
