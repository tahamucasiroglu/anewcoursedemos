namespace OkulYonetim.Domain.Organization;

/// <summary>Akademik takvim kaydının türü (spec 0008): tatil, sınav, etkinlik. Ekranlarda tür metinle de yazılır.</summary>
public enum CalendarEntryKind
{
    Holiday = 1,
    Exam = 2,
    Event = 3,
}
