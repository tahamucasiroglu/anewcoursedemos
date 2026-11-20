using OkulYonetim.Application.Features.Communication.Announcements;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Communication;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Communication;

/// <summary>
/// Spec 0011 AC-6 ve AC-5 kuralları: onay cevabı son gün (Türkiye günü) dahil verilir ve değiştirilir — son gün 23:59
/// (UTC 20:59) kabul, ertesi gün 00:00 (UTC 21:00) red; son cevap ve cevaplayan geçerlidir; onay formu olmayan duyuruya
/// cevap verilemez. Son gün yarından en geç 180 gün sonrasına (bugün ve 181. gün dışarıda).
/// </summary>
public sealed class ConsentResponseTests
{
    private static readonly DateTimeOffset Published = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Deadline = new(2026, 11, 27);

    private static Announcement Form(string? question = "Geziye katılsın mı?") =>
        Announcement.Publish(Guid.NewGuid(), Guid.NewGuid(), AnnouncementScope.Classroom, Guid.NewGuid(), "Gezi", "Metin", question, Deadline, Published);

    [Theory]
    [InlineData(20, 59, true)]
    [InlineData(21, 0, false)]
    public void AcceptsConsentOn_AroundDeadlineMidnight_UsesTurkeyDay(int utcHour, int utcMinute, bool expected)
    {
        DateOnly turkeyDay = SchoolCalendar.TurkeyDay(new DateTimeOffset(2026, 11, 27, utcHour, utcMinute, 0, TimeSpan.Zero), null);

        Form().AcceptsConsentOn(turkeyDay).ShouldBe(expected);
    }

    [Fact]
    public void Change_BeforeDeadline_LastAnswerAndAnswererWin()
    {
        Announcement form = Form();
        Guid student = Guid.NewGuid();
        Guid mother = Guid.NewGuid();
        Guid father = Guid.NewGuid();
        ConsentResponse response = ConsentResponse.Give(form, student, ConsentAnswer.Approved, mother, Published, new DateOnly(2026, 11, 20));

        response.Change(form, ConsentAnswer.Declined, father, Published.AddDays(7), Deadline);

        response.Answer.ShouldBe(ConsentAnswer.Declined);
        response.AnsweredByUserId.ShouldBe(father);
        response.AnsweredAt.ShouldBe(Published.AddDays(7));
    }

    [Fact]
    public void GiveAndChange_AfterDeadline_Throw()
    {
        Announcement form = Form();
        ConsentResponse response = ConsentResponse.Give(form, Guid.NewGuid(), ConsentAnswer.Approved, Guid.NewGuid(), Published, Deadline);

        Should.Throw<InvalidOperationException>(() => ConsentResponse.Give(form, Guid.NewGuid(), ConsentAnswer.Approved, Guid.NewGuid(), Published, Deadline.AddDays(1)));
        Should.Throw<InvalidOperationException>(() => response.Change(form, ConsentAnswer.Declined, Guid.NewGuid(), Published, Deadline.AddDays(1)));
        response.Answer.ShouldBe(ConsentAnswer.Approved);
    }

    [Fact]
    public void Give_OnAnnouncementWithoutForm_Throws()
    {
        Announcement plain = Form(question: null);

        plain.RequiresConsent.ShouldBeFalse();
        plain.ConsentDeadline.ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => ConsentResponse.Give(plain, Guid.NewGuid(), ConsentAnswer.Approved, Guid.NewGuid(), Published, new DateOnly(2026, 11, 20)));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(180, true)]
    [InlineData(181, false)]
    public void IsDeadlineInRange_TomorrowTo180Days(int days, bool expected)
    {
        DateOnly today = new(2026, 11, 20);

        PublishAnnouncementCommandValidator.IsDeadlineInRange(today.AddDays(days), today).ShouldBe(expected);
    }
}
