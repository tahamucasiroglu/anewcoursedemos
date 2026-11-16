namespace Konsept.Agent.Domain.Enums;

/// <summary>Kurulum olayı türü (spec 0009 Req 5, 12, 17).</summary>
public enum SetupEventKind
{
    /// <summary>Kurulum başladı.</summary>
    Started = 0,

    /// <summary>Bir bileşenin durumu değişti.</summary>
    StatusChanged = 1,

    /// <summary>İndirme otomatik yeniden deneniyor.</summary>
    Retrying = 2,

    /// <summary>Resmi kaynak erişilemedi; Pi aynasından indiriliyor.</summary>
    UsingMirror = 3,

    /// <summary>Hazır denetimi sürüyor.</summary>
    Checking = 4,

    /// <summary>Kurulum tamamlandı · Hazır.</summary>
    Ready = 5,

    /// <summary>Kurulum durdu (iptal ya da bir bileşen hatasıyla).</summary>
    Stopped = 6,

    /// <summary>İndirme ilerlemesi değişti.</summary>
    Progress = 7,
}
