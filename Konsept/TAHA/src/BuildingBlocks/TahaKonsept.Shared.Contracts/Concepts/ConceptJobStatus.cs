namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konsept üretim işinin durumu. <see cref="Idle"/>: kullanıcının hiç işi yok.
/// </summary>
public enum ConceptJobStatus
{
    /// <summary>İş yok.</summary>
    Idle = 0,

    /// <summary>Sırada bekliyor.</summary>
    Queued = 1,

    /// <summary>Çalışıyor.</summary>
    Running = 2,

    /// <summary>Bitti (bazı öğeler başarısız olmuş olabilir).</summary>
    Completed = 3,

    /// <summary>Başarısız (hiç konsept üretilemedi ya da iş durduruldu).</summary>
    Failed = 4,

    /// <summary>Kullanıcı iptal etti.</summary>
    Cancelled = 5,
}
