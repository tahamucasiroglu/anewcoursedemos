using OkulYonetim.Application.Features.Academics.Assignments;
using OkulYonetim.Domain.Academics;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Academics;

/// <summary>
/// Spec 0013 AC-1 kuralları (birim): ders 60, başlık 150, açıklama 2000 karakter sınırda kabul, bir fazlası red; boş ders/başlık
/// red; teslim günü bugün ve 90. gün kabul, dün ve 91. gün red; ödev "İşaretlenmedi" satırlarıyla başlar ve durum değişince
/// değiştiren ve zaman yazılır. Teslim durumu metni yalnızca üç adı kabul eder (sayı yok).
/// </summary>
public sealed class AssignmentTests
{
    private static readonly DateOnly Today = new(2026, 11, 20);
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    private static Assignment Make(string subject = "Matematik", string title = "Kesirler alıştırması", string? description = null, DateOnly? due = null) =>
        Assignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), subject, title, description, due ?? Today.AddDays(1), Today, Now);

    [Fact]
    public void Create_AtLimits_Accepted()
    {
        Assignment assignment = Make(new string('m', 60), new string('b', 150), new string('a', 2000), Today.AddDays(90));

        assignment.Subject.Length.ShouldBe(60);
        assignment.Title.Length.ShouldBe(150);
        assignment.Description!.Length.ShouldBe(2000);
        Make(due: Today).DueDate.ShouldBe(Today);
    }

    [Theory]
    [InlineData(61, 10, 0)]
    [InlineData(10, 151, 0)]
    [InlineData(10, 10, 2001)]
    [InlineData(0, 10, 0)]
    [InlineData(10, 0, 0)]
    public void Create_OverOrEmpty_Throws(int subject, int title, int description)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Make(new string('m', subject), new string('b', title), new string('a', description)));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(90, true)]
    [InlineData(91, false)]
    public void IsDueDateAllowed_TodayTo90Days(int offset, bool expected)
    {
        Assignment.IsDueDateAllowed(Today.AddDays(offset), Today).ShouldBe(expected);
        if (!expected)
        {
            Should.Throw<ArgumentOutOfRangeException>(() => Make(due: Today.AddDays(offset)));
        }
    }

    [Fact]
    public void Submission_StartsUnmarked_SetRecordsChangeOnlyWhenDifferent()
    {
        Assignment assignment = Make();
        AssignmentSubmission submission = AssignmentSubmission.Start(assignment, Guid.NewGuid(), Now);
        Guid teacher = Guid.NewGuid();

        submission.Status.ShouldBe(SubmissionStatus.Unmarked);
        submission.Set(SubmissionStatus.NotSubmitted, teacher, Now.AddHours(1)).ShouldBeTrue();
        submission.Set(SubmissionStatus.NotSubmitted, teacher, Now.AddHours(2)).ShouldBeFalse();
        submission.Status.ShouldBe(SubmissionStatus.NotSubmitted);
        submission.UpdatedByUserId.ShouldBe(teacher);
        submission.UpdatedAt.ShouldBe(Now.AddHours(1));
    }

    [Theory]
    [InlineData("Submitted", true)]
    [InlineData("notsubmitted", true)]
    [InlineData("Unmarked", true)]
    [InlineData("1", false)]
    [InlineData("Late", false)]
    [InlineData(null, false)]
    public void TryParseStatus_OnlyNamedStatuses(string? text, bool expected)
    {
        SaveSubmissionsCommandValidator.TryParseStatus(text, out _).ShouldBe(expected);
    }
}
