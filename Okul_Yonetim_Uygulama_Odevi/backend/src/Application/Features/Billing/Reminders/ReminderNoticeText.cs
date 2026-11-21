using System.Text.RegularExpressions;

namespace OkulYonetim.Application.Features.Billing.Reminders;

/// <summary>
/// Gecikme hatırlatmasının metni (spec 0015 gereksinim 9): YAML şablonundaki <c>{student}</c>, <c>{due}</c> (gün.ay.yıl) ve
/// <c>{remaining}</c> ("1.234,50") yer tutucuları şablon üzerinde tek geçişte doldurulur — öğrenci adında yazan <c>{due}</c> gibi bir
/// metin yeniden genişlemez (0013 review R1-2 kalıbı, <c>AssignmentNoticeText</c>).
/// </summary>
public static partial class ReminderNoticeText
{
    public static string Render(string template, string student, DateOnly due, decimal remaining)
    {
        ArgumentNullException.ThrowIfNull(template);
        string day = Billing.MoneyText.Day(due);
        string amount = Billing.MoneyText.Format(remaining);
        return Placeholder().Replace(template, m => m.Groups[1].Value switch
        {
            "student" => student,
            "due" => day,
            _ => amount,
        });
    }

    [GeneratedRegex(@"\{(student|due|remaining)\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();
}
