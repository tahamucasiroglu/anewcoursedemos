using OkulYonetim.Application.Features.Academics.Assessments;
using OkulYonetim.Domain.Academics;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Academics;

/// <summary>
/// Spec 0013 AC-6 ve AC-5 kuralları (birim): puan 0 ve 100 kabul, -1 ve 101 red; yazılan puanda ondalık, işaret ve sayı
/// olmayan değer red, boş = girilmedi; puan düzeltilir ve silinir (boş). Değerlendirme başlığı 100, ders adı 60 sınırında kabul;
/// gün bugünden sonra olamaz; tür yalnızca Yazılı/Sözlü.
/// </summary>
public sealed class GradeTests
{
    private static readonly DateOnly Today = new(2026, 11, 20);
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    private static Assessment Make(string subject = "Türkçe", string title = "1. yazılı", DateOnly? date = null, AssessmentKind kind = AssessmentKind.Written) =>
        Assessment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), subject, kind, title, date ?? Today, Today, Now);

    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void SetScore_Bounds(int score, bool accepted)
    {
        Grade grade = Grade.Start(Make(), Guid.NewGuid(), Now);
        if (accepted)
        {
            grade.SetScore(score, Guid.NewGuid(), Now).ShouldBeTrue();
            grade.Score.ShouldBe(score);
        }
        else
        {
            Should.Throw<ArgumentOutOfRangeException>(() => grade.SetScore(score, Guid.NewGuid(), Now));
            grade.Score.ShouldBeNull();
        }
    }

    [Fact]
    public void SetScore_CorrectAndClear_OnlyChangesCount()
    {
        Grade grade = Grade.Start(Make(), Guid.NewGuid(), Now);
        Guid teacher = Guid.NewGuid();

        grade.Score.ShouldBeNull();
        grade.SetScore(85, teacher, Now).ShouldBeTrue();
        grade.SetScore(85, teacher, Now).ShouldBeFalse();
        grade.SetScore(90, teacher, Now).ShouldBeTrue();
        grade.SetScore(null, teacher, Now.AddHours(1)).ShouldBeTrue();
        grade.Score.ShouldBeNull();
        grade.UpdatedAt.ShouldBe(Now.AddHours(1));
    }

    [Theory]
    [InlineData(null, true, null)]
    [InlineData("  ", true, null)]
    [InlineData("0", true, 0)]
    [InlineData("100", true, 100)]
    [InlineData(" 85 ", true, 85)]
    [InlineData("-1", false, null)]
    [InlineData("101", false, null)]
    [InlineData("85.5", false, null)]
    [InlineData("85,5", false, null)]
    [InlineData("abc", false, null)]
    [InlineData("+5", false, null)]
    [InlineData("1000", false, null)]
    public void TryParseScore_IntegerZeroToHundredOrEmpty(string? text, bool valid, int? expected)
    {
        SaveGradesCommandValidator.TryParseScore(text, out int? score).ShouldBe(valid);
        score.ShouldBe(expected);
    }

    [Fact]
    public void Assessment_LimitsDateAndKind()
    {
        Make(new string('d', 60), new string('b', 100)).Title.Length.ShouldBe(100);
        Should.Throw<ArgumentOutOfRangeException>(() => Make(new string('d', 61)));
        Should.Throw<ArgumentOutOfRangeException>(() => Make(title: new string('b', 101)));
        Should.Throw<ArgumentOutOfRangeException>(() => Make(title: " "));
        Should.Throw<ArgumentOutOfRangeException>(() => Make(date: Today.AddDays(1)));
        Should.Throw<ArgumentOutOfRangeException>(() => Make(kind: (AssessmentKind)7));
        CreateAssessmentCommandValidator.TryParseKind("oral", out AssessmentKind oral).ShouldBeTrue();
        oral.ShouldBe(AssessmentKind.Oral);
        CreateAssessmentCommandValidator.TryParseKind("1", out _).ShouldBeFalse();
        CreateAssessmentCommandValidator.TryParseKind("Quiz", out _).ShouldBeFalse();
    }
}
