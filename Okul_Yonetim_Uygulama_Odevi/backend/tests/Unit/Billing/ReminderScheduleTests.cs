using OkulYonetim.Domain.Billing;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Billing;

/// <summary>
/// Spec 0015 AC-10 kuralları (birim): vade+1. gün 10:00'dan (Türkiye saati) önce hiçbir şey; 10:00'da 1. hatırlatma; vade+8'de 2.,
/// vade+15'te 3.; 4. hiç yok; aynı sıra iki kez yok; birkaç gün kapalı kalan sistem açılınca yalnızca bir hatırlatma (vakti gelmiş
/// en büyük sıra) — atlananlar birikmez; vade günü ve öncesi hiçbir şey.
/// </summary>
public sealed class ReminderScheduleTests
{
    private static readonly DateOnly Due = new(2026, 11, 10);
    private static readonly TimeSpan Turkey = TimeSpan.FromHours(3);

    [Fact]
    public void Due_DayAfterBeforeTen_Nothing_AtTen_First()
    {
        ReminderSchedule.Due(Due, At(11, 9, 59), 0).ShouldBeNull();
        ReminderSchedule.Due(Due, At(11, 10, 0), 0).ShouldBe(1);
        ReminderSchedule.Due(Due, At(11, 23, 0), 0).ShouldBe(1);
    }

    [Fact]
    public void Due_OnDueDayAndBefore_Nothing()
    {
        ReminderSchedule.Due(Due, At(10, 15, 0), 0).ShouldBeNull();
        ReminderSchedule.Due(Due, At(1, 15, 0), 0).ShouldBeNull();
    }

    [Fact]
    public void Due_SequenceOneTwoThree_ThenNeverFourth()
    {
        ReminderSchedule.Due(Due, At(12, 11, 0), 1).ShouldBeNull("aynı sıra iki kez gitmez");
        ReminderSchedule.Due(Due, At(17, 11, 0), 1).ShouldBeNull();
        ReminderSchedule.Due(Due, At(18, 9, 0), 1).ShouldBeNull();
        ReminderSchedule.Due(Due, At(18, 10, 30), 1).ShouldBe(2);
        ReminderSchedule.Due(Due, At(25, 10, 30), 2).ShouldBe(3);
        ReminderSchedule.Due(Due, At(25, 10, 30), 3).ShouldBeNull();
        ReminderSchedule.Due(Due, new DateTimeOffset(2027, 2, 1, 12, 0, 0, Turkey), 3).ShouldBeNull("en fazla 3");
    }

    [Fact]
    public void Due_SystemDownSeveralDays_OnlyOneReminderNoBacklog()
    {
        ReminderSchedule.Due(Due, At(20, 11, 0), 0).ShouldBe(2, "1. kaçırıldı; vakti gelmiş en büyük sıra tek başına gider");
        ReminderSchedule.Due(Due, At(21, 11, 0), 2).ShouldBeNull("atlanan 1. sonradan gönderilmez");
        ReminderSchedule.Due(Due, new DateTimeOffset(2026, 12, 20, 11, 0, 0, Turkey), 0).ShouldBe(3);
    }

    private static DateTimeOffset At(int day, int hour, int minute) => new(2026, 11, day, hour, minute, 0, Turkey);
}
