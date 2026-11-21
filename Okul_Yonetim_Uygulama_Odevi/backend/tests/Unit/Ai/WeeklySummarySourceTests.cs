using OkulYonetim.Application.Features.Academics.Records;
using OkulYonetim.Application.Features.Ai.Prompts;
using OkulYonetim.Application.Features.Ai.WeeklySummaries;
using OkulYonetim.Application.Features.Attendance;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Ai;

/// <summary>
/// Spec 0014 AC-5 ve AC-2 (birim): modele giden içerik yalnızca öğrencinin adını (soyadı değil), haftayı, yoklama sayıları ve
/// günlerini, ödev ve not satırlarını ve öğretmen notunu taşır; ödev/not yoksa o satırlar yer almaz; dayanak satırı spec'teki
/// biçimde ("Dayanak: 4 gün geldi, 1 gün gelmedi; 2 ödev; 1 not; öğretmen notu"); dayanak boş mu kuralı; JSON'a yazılıp aynen okunur;
/// talimat gömülü dosyayla aynı ve kişisel yer tutucu taşımaz.
/// </summary>
public sealed class WeeklySummarySourceTests
{
    private static readonly WeeklySummaryBasisTexts Texts = new("Dayanak: ", "{present} gün geldi{late}, {absent} gün gelmedi", " ({late} gün geç)", "yoklama yok", "{count} ödev", "{count} not", "öğretmen notu");

    private static readonly DateOnly Monday = new(2026, 11, 16);

    [Theory]
    [InlineData("Ali Yılmaz", "Ali")]
    [InlineData("Ali Kemal Yılmaz", "Ali Kemal")]
    [InlineData("  Zeynep   Su  Ak ", "Zeynep Su")]
    [InlineData("Ece", "Ece")]
    public void FirstNameOf_DropsSurname(string fullName, string expected) =>
        WeeklySummarySource.FirstNameOf(fullName).ShouldBe(expected);

    [Fact]
    public void ToPromptText_FullWeek_NameBasisAndNoteOnly()
    {
        WeeklySummarySource source = Full();

        string text = source.ToPromptText();

        text.ShouldContain("Öğrenci: Ali");
        text.ShouldNotContain("Yılmaz");
        text.ShouldContain("Hafta: 16–22 Kasım");
        text.ShouldContain("5 gün yoklama alındı; 3 gün zamanında geldi, 1 gün geç geldi, 1 gün gelmedi.");
        text.ShouldContain("Gelmediği günler: 18 Kasım Çarşamba");
        text.ShouldContain("Geç geldiği günler: 19 Kasım Perşembe");
        text.ShouldContain("- Matematik: Kesirler (teslim 20 Kasım Cuma) — teslim etti");
        text.ShouldContain("- Fen: Deney raporu (teslim 18 Kasım Çarşamba) — teslim etmedi");
        text.ShouldContain("- Türkçe yazılı: 1. yazılı (17 Kasım Salı) — 85 puan");
        text.ShouldContain("Öğretmen notu: Derse katılımı arttı.");
        text.ShouldNotContain(Guid.Empty.ToString());
    }

    [Fact]
    public void ToPromptText_NoAcademicsNoNote_SectionsLeftOut()
    {
        WeeklySummarySource source = new("Ali", Monday, new StudentWeekAttendance(2, 2, 0, 0, [], []), [], [], null);

        string text = source.ToPromptText();

        text.ShouldContain("2 gün yoklama alındı");
        text.ShouldNotContain("Ödevler");
        text.ShouldNotContain("Notlar");
        text.ShouldNotContain("Öğretmen notu");
        text.ShouldNotContain("Gelmediği");
    }

    [Fact]
    public void ToBasisLine_FullWeek_SpecFormat()
    {
        WeeklySummarySource source = Full() with { Attendance = new StudentWeekAttendance(5, 4, 1, 0, [new DateOnly(2026, 11, 18)], []), Grades = [Full().Grades[0]] };

        source.ToBasisLine(Texts).ShouldBe("Dayanak: 4 gün geldi, 1 gün gelmedi; 2 ödev; 1 not; öğretmen notu");
        Full().ToBasisLine(Texts).ShouldBe("Dayanak: 4 gün geldi (1 gün geç), 1 gün gelmedi; 2 ödev; 1 not; öğretmen notu");
    }

    [Fact]
    public void ToBasisLine_OnlyNote_NoAttendanceAndNote()
    {
        WeeklySummarySource source = new("Ali", Monday, StudentWeekAttendance.None, [], [], "Kitap okudu.");

        source.IsEmpty.ShouldBeFalse();
        source.ToBasisLine(Texts).ShouldBe("Dayanak: yoklama yok; öğretmen notu");
    }

    [Fact]
    public void IsEmpty_NoDataNoNote_True_WhitespaceNoteStillEmpty()
    {
        new WeeklySummarySource("Ali", Monday, StudentWeekAttendance.None, [], [], null).IsEmpty.ShouldBeTrue();
        new WeeklySummarySource("Ali", Monday, StudentWeekAttendance.None, [], [], "   ").IsEmpty.ShouldBeTrue();
        new WeeklySummarySource("Ali", Monday, new StudentWeekAttendance(1, 1, 0, 0, [], []), [], [], null).IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void Json_RoundTrip_SameBasis()
    {
        WeeklySummarySource source = Full();

        WeeklySummarySource back = WeeklySummarySource.FromJson(source.ToJson());

        back.ToPromptText().ShouldBe(source.ToPromptText());
        back.ToBasisLine(Texts).ShouldBe(source.ToBasisLine(Texts));
        back.Attendance.AbsentDays.ShouldBe(source.Attendance.AbsentDays);
    }

    [Fact]
    public void WeeklySummaryPrompt_EmbeddedEqualsSourceFileAndHasRules()
    {
        string file = File.ReadAllText(Path.Combine(BackendRoot(), "src", "Application", "Features", "Ai", "Prompts", "weekly-summary.tr.txt"))
            .Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

        AiPrompts.WeeklySummary.ShouldBe(file);
        AiPrompts.WeeklySummary.ShouldContain("3 ile 5 cümle");
        AiPrompts.WeeklySummary.ShouldContain("Yalnızca verilen veriye dayan");
        AiPrompts.WeeklySummary.ShouldNotContain("{");
        AiPrompts.WeeklySummaryMaxTokens.ShouldBeInRange(100, 1000);
    }

    private static WeeklySummarySource Full() => new(
        "Ali",
        Monday,
        new StudentWeekAttendance(5, 3, 1, 1, [new DateOnly(2026, 11, 18)], [new DateOnly(2026, 11, 19)]),
        [
            new StudentAssignmentRecord(Guid.NewGuid(), "Fen", "Deney raporu", new DateOnly(2026, 11, 18), "NotSubmitted"),
            new StudentAssignmentRecord(Guid.NewGuid(), "Matematik", "Kesirler", new DateOnly(2026, 11, 20), "Submitted"),
        ],
        [new StudentGradeRecord(Guid.NewGuid(), "Türkçe", "Written", "1. yazılı", new DateOnly(2026, 11, 17), 85)],
        "Derse katılımı arttı.");

    private static string BackendRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("backend/Directory.Build.props bulunamadı");
    }
}
