using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using OkulYonetim.Application.Features.Academics.Records;
using OkulYonetim.Application.Features.Attendance;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// Haftalık gelişim özetinin dayanağı (spec 0014 gereksinim 2–4, BR-6): öğrencinin <b>yalnızca adı</b> (soyadı değil —
/// <see cref="FirstNameOf"/>), haftanın Pazartesi'si, o haftanın yoklama özeti (Attendance), teslim günü o haftada olan ödevler ve
/// günü o haftada olan notlar (Academics; yoksa boş) ve öğretmenin isteğe bağlı notu. Sağlık, alerji, veli ve başka öğrenci verisi
/// bu tipte yoktur — modele giden içerik (<see cref="ToPromptText"/>) yalnızca buradan kurulur (veri minimizasyonu, AC-5).
/// Taslakla birlikte JSON olarak saklanır (<see cref="ToJson"/>/<see cref="FromJson"/>); veliye ve öğretmene kısa dayanak satırı
/// <see cref="ToBasisLine"/> ("Dayanak: 4 gün geldi, 1 gün gelmedi; 2 ödev; 1 not; öğretmen notu"). <see cref="IsEmpty"/>:
/// yoklama, ödev, not ve öğretmen notu yok — özet üretilmez (AC-4). Modele giden etiketler (Öğrenci:, Yoklama:…) kullanıcıya
/// gösterilmez; talimatla birlikte modelin girdisidir.
/// </summary>
public sealed record WeeklySummarySource(
    string FirstName,
    DateOnly WeekStart,
    StudentWeekAttendance Attendance,
    IReadOnlyList<StudentAssignmentRecord> Assignments,
    IReadOnlyList<StudentGradeRecord> Grades,
    string? TeacherNote)
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    public bool IsEmpty => this.Attendance.DaysTaken == 0 && this.Assignments.Count == 0 && this.Grades.Count == 0 && string.IsNullOrWhiteSpace(this.TeacherNote);

    /// <summary>Ad-soyaddan yalnızca ad: son kelime soyadı sayılır ve atılır ("Ali Kemal Yılmaz" → "Ali Kemal"); tek kelimeyse kendisi.</summary>
    public static string FirstNameOf(string fullName)
    {
        ArgumentNullException.ThrowIfNull(fullName);
        string[] words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.Length <= 1 ? string.Join(' ', words) : string.Join(' ', words[..^1]);
    }

    public static WeeklySummarySource FromJson(string json) =>
        JsonSerializer.Deserialize<WeeklySummarySource>(json, Json) ?? throw new InvalidOperationException("Haftalık özet dayanağı okunamadı.");

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    /// <summary>Modele giden içerik: ad, hafta, yoklama sayıları ve günleri, varsa ödev ve not satırları, varsa öğretmen notu.</summary>
    public string ToPromptText()
    {
        SchoolWeek week = SchoolWeek.StartingOn(this.WeekStart);
        StringBuilder text = new();
        text.Append("Öğrenci: ").AppendLine(this.FirstName);
        text.Append("Hafta: ").AppendLine(week.Label());
        StudentWeekAttendance a = this.Attendance;
        if (a.DaysTaken == 0)
        {
            text.AppendLine("Yoklama: bu hafta yoklama kaydı yok.");
        }
        else
        {
            text.Append(CultureInfo.InvariantCulture, $"Yoklama: {a.DaysTaken} gün yoklama alındı; {a.Present} gün zamanında geldi, {a.Late} gün geç geldi, {a.Absent} gün gelmedi.").AppendLine();
            if (a.AbsentDays.Count > 0)
            {
                text.Append("Gelmediği günler: ").AppendLine(string.Join(", ", a.AbsentDays.Select(Day)));
            }

            if (a.LateDays.Count > 0)
            {
                text.Append("Geç geldiği günler: ").AppendLine(string.Join(", ", a.LateDays.Select(Day)));
            }
        }

        if (this.Assignments.Count > 0)
        {
            text.AppendLine("Ödevler:");
            foreach (StudentAssignmentRecord assignment in this.Assignments)
            {
                text.Append("- ").Append(assignment.Subject).Append(": ").Append(assignment.Title)
                    .Append(" (teslim ").Append(Day(assignment.DueDate)).Append(") — ").AppendLine(SubmissionText(assignment.Status));
            }
        }

        if (this.Grades.Count > 0)
        {
            text.AppendLine("Notlar:");
            foreach (StudentGradeRecord grade in this.Grades)
            {
                text.Append("- ").Append(grade.Subject).Append(' ').Append(grade.Kind == "Oral" ? "sözlü" : "yazılı").Append(": ").Append(grade.Title)
                    .Append(" (").Append(Day(grade.Date)).Append(") — ")
                    .AppendLine(grade.Score is { } score ? score.ToString(CultureInfo.InvariantCulture) + " puan" : "puan girilmedi");
            }
        }

        if (!string.IsNullOrWhiteSpace(this.TeacherNote))
        {
            text.Append("Öğretmen notu: ").AppendLine(this.TeacherNote.Trim());
        }

        return text.ToString().TrimEnd();
    }

    /// <summary>Kısa dayanak satırı (veliye giden bildirimde ve öğretmen panelinde).</summary>
    public string ToBasisLine(WeeklySummaryBasisTexts texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        StudentWeekAttendance a = this.Attendance;
        List<string> parts = [];
        if (a.DaysTaken == 0)
        {
            parts.Add(texts.NoAttendance);
        }
        else
        {
            string late = a.Late > 0 ? texts.Late.Replace("{late}", Number(a.Late), StringComparison.Ordinal) : string.Empty;
            parts.Add(texts.Attendance
                .Replace("{present}", Number(a.Present + a.Late), StringComparison.Ordinal)
                .Replace("{absent}", Number(a.Absent), StringComparison.Ordinal)
                .Replace("{late}", late, StringComparison.Ordinal));
        }

        if (this.Assignments.Count > 0)
        {
            parts.Add(texts.Assignments.Replace("{count}", Number(this.Assignments.Count), StringComparison.Ordinal));
        }

        if (this.Grades.Count > 0)
        {
            parts.Add(texts.Grades.Replace("{count}", Number(this.Grades.Count), StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(this.TeacherNote))
        {
            parts.Add(texts.TeacherNote);
        }

        return texts.Prefix + string.Join("; ", parts);
    }

    private static string Day(DateOnly day) => day.ToString("d MMMM dddd", Turkish);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string SubmissionText(string status) => status switch
    {
        "Submitted" => "teslim etti",
        "NotSubmitted" => "teslim etmedi",
        _ => "teslim durumu işaretlenmedi",
    };
}
