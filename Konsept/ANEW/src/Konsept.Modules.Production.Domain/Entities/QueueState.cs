namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>
/// Kullanıcının sırasının hat durumu (spec 0017 Req 11): art arda hata sayısı ve sıranın 3 hatada kendiliğinden durup durmadığı (uyarı).
/// Başarılı bir iş sayacı sıfırlar; yeni bir Sırayla üret ya da Devam et uyarıyı kaldırır.
/// </summary>
public sealed record QueueState
{
    /// <summary>Gets sahibi (birincil anahtar).</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets art arda hata sayısı.</summary>
    public int ConsecutiveFailures { get; init; }

    /// <summary>Gets a value indicating whether sıra 3 hatada durdu.</summary>
    public bool Halted { get; init; }

    /// <summary>Gets son güncelleme (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
