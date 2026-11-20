namespace OkulYonetim.Domain.Communication;

/// <summary>Veli bildiriminin türü (spec 0010): devamsızlık ya da "yoklama düzeltildi: okulda".</summary>
public enum GuardianNoticeKind
{
    Absent = 1,
    Corrected = 2,
}
