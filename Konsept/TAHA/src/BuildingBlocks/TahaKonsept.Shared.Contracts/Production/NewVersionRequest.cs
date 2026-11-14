namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Yeni sürüm isteği (<c>POST api/production/videos/{id}/new-version</c>): verilen (ya da güncel) ayarlarla yeni iş açılır;
/// girdi hash'i değişmeyen aşamalar kaynak işten yeniden kullanılır, yalnızca değişen aşamalar ve bağımlıları çalışır.
/// </summary>
public sealed record NewVersionRequest
{
    /// <summary>Yeni ayarlar (<c>null</c> = kullanıcının güncel ayarları).</summary>
    public ProductionSettingsDto? Settings { get; init; }

    /// <summary>Kullanıcının işaretlediği değişiklikler (bilgi amaçlı; karar hash ile verilir).</summary>
    public IReadOnlyList<string> Changed { get; init; } = [];

    /// <summary>Not.</summary>
    public string? Note { get; init; }
}
