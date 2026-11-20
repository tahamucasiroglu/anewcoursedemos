namespace OkulYonetim.Domain.Communication;

/// <summary>Öğretmenin mesai günleri (spec 0011 C-6): haftanın günleri bayrak olarak; en az biri seçilir.</summary>
[Flags]
public enum WorkDays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
}
