namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Dosya eylemi cevabı: başarı ya da hata kodu (<see cref="NotFound"/>, <see cref="Failed"/>).</summary>
/// <param name="Ok">Açıldıysa <c>true</c>.</param>
/// <param name="Error">Hata kodu ya da <c>null</c>.</param>
public sealed record AgentFileActionReply(bool Ok, string? Error)
{
    /// <summary>Video dosyası ajan klasöründe yok.</summary>
    public const string NotFound = "not_found";

    /// <summary>Ajan dosyayı açamadı (geçersiz eylem, kabuk hatası, sözleşme uyumsuzluğu).</summary>
    public const string Failed = "failed";
}
