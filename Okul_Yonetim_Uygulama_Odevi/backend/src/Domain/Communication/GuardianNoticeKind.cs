namespace OkulYonetim.Domain.Communication;

/// <summary>Veli bildiriminin türü (spec 0010): devamsızlık ya da "yoklama düzeltildi: okulda"; spec 0013: yeni ödev (kayda bağlı okul bildirimi).</summary>
public enum GuardianNoticeKind
{
    Absent = 1,
    Corrected = 2,
    Assignment = 3,
}
