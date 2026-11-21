using System.Globalization;

namespace OkulYonetim.Domain.Ai;

/// <summary>
/// Haftalık gelişim özetinin haftası (spec 0014 C-2, gereksinim 2): Pazartesi 00:00 dahil – sonraki Pazartesi 00:00 hariç,
/// Türkiye günüyle (gün, çağıranın Türkiye saatiyle hesapladığı <see cref="DateOnly"/>'dir — <c>SchoolCalendar.Today</c>).
/// <see cref="Start"/> Pazartesi, <see cref="End"/> Pazar (dahil); "bu hafta" bugünü kapsayan hafta, "geçen hafta" ondan önceki
/// Pazartesi–Pazar. <see cref="Label"/> veliye giden başlıktaki aralık: "16–22 Kasım", ay değişirse "30 Kasım – 6 Aralık".
/// </summary>
public readonly record struct SchoolWeek
{
    public const string Current = "current";
    public const string Previous = "previous";

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private SchoolWeek(DateOnly start)
    {
        this.Start = start;
    }

    public DateOnly Start { get; }

    public DateOnly End => this.Start.AddDays(6);

    /// <summary>Verilen günü kapsayan hafta (Pazartesi başlar).</summary>
    public static SchoolWeek Containing(DateOnly day)
    {
        int sinceMonday = ((int)day.DayOfWeek + 6) % 7;
        return new SchoolWeek(day.AddDays(-sinceMonday));
    }

    /// <summary>"current" (bu hafta) ya da "previous" (geçen hafta) seçimi; başka değer geçersizdir.</summary>
    public static bool IsValidChoice(string? choice) => choice is Current or Previous;

    /// <summary>Bugüne göre seçilen hafta: "previous" geçen hafta, diğerleri bu hafta.</summary>
    public static SchoolWeek Choose(string? choice, DateOnly today)
    {
        SchoolWeek current = Containing(today);
        return choice == Previous ? current.Before() : current;
    }

    /// <summary>Haftanın Pazartesi'siyle hafta (Pazartesi değilse <see cref="ArgumentOutOfRangeException"/>).</summary>
    public static SchoolWeek StartingOn(DateOnly monday) =>
        monday.DayOfWeek == DayOfWeek.Monday ? new SchoolWeek(monday) : throw new ArgumentOutOfRangeException(nameof(monday), "Hafta Pazartesi başlar.");

    /// <summary>Bir önceki hafta.</summary>
    public SchoolWeek Before() => new(this.Start.AddDays(-7));

    public bool Contains(DateOnly day) => day >= this.Start && day <= this.End;

    /// <summary>Aralık metni: aynı ayda "16–22 Kasım", ay değişince "30 Kasım – 6 Aralık".</summary>
    public string Label() =>
        this.Start.Month == this.End.Month
            ? this.Start.Day.ToString(Turkish) + "–" + this.End.ToString("d MMMM", Turkish)
            : this.Start.ToString("d MMMM", Turkish) + " – " + this.End.ToString("d MMMM", Turkish);
}
