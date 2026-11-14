namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// İşin tek aşama çalıştırması (sahne bazlı aşamalarda sahne başına bir kayıt): hash, durum, yer, maliyet, süre, hata.
/// </summary>
public sealed record StageRunDto
{
    /// <summary>Kimlik.</summary>
    public Guid Id { get; init; }

    /// <summary>Aşama.</summary>
    public PipelineStage Stage { get; init; }

    /// <summary>Sahne indeksi (tüm video için <c>null</c>).</summary>
    public int? SceneIndex { get; init; }

    /// <summary>Durum.</summary>
    public StageStatus Status { get; init; }

    /// <summary>Çalıştığı yer.</summary>
    public StageLocation Location { get; init; }

    /// <summary>Girdi hash'i (SHA-256).</summary>
    public string InputHash { get; init; } = string.Empty;

    /// <summary>Çıktı hash'i.</summary>
    public string? OutputHash { get; init; }

    /// <summary>Önceki sürümden yeniden kullanıldı mı.</summary>
    public bool Reused { get; init; }

    /// <summary>İlerleme (0-100).</summary>
    public double Progress { get; init; }

    /// <summary>Son mesaj.</summary>
    public string? Message { get; init; }

    /// <summary>Deneme sayısı.</summary>
    public int Attempts { get; init; }

    /// <summary>Giriş token'ı (LLM).</summary>
    public long InputTokens { get; init; }

    /// <summary>Çıkış token'ı (LLM).</summary>
    public long OutputTokens { get; init; }

    /// <summary>Maliyet ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Başlama (UTC).</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>Bitiş (UTC).</summary>
    public DateTimeOffset? FinishedAt { get; init; }

    /// <summary>Süre (ms).</summary>
    public long DurationMs { get; init; }

    /// <summary>Hata kodu.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Hata mesajı.</summary>
    public string? ErrorMessage { get; init; }
}
