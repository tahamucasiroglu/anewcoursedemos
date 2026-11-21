using OkulYonetim.Application.Features.Appointments;
using OkulYonetim.Domain.Appointments;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Appointments;

/// <summary>
/// Spec 0016 AC-1 (dilim hesabı): 14:00–16:00 / 20 dk → 6 dilim, 14:00–14:50 / 20 dk → 2 dilim (kalan 10 dk dilim olmaz); geçmiş gün,
/// 61. gün, başlangıç ≥ bitiş, 15 dk adımı dışı saat, 8 saati aşan aralık ve 15/20/30 dışı süre geçersiz; bugünün gelecek saati ve 60. gün
/// kabul; başlamış saatlere düşen dilim oluşmaz. Türkiye saati: 14:00 Türkiye = 11:00 UTC (yaz saati yok; saat dilimi bulunamazsa sabit
/// UTC+3 aynı sonucu verir). Bildirim metni yer tutucuları tek geçişte doldurulur.
/// </summary>
public sealed class AvailabilityPlannerTests
{
    // 2026-11-20 Cuma 09:00 UTC = Türkiye 12:00.
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 20);
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    [Fact]
    public void Plan_TwoHoursTwentyMinutes_SixSlotsInTurkeyTime()
    {
        IReadOnlyList<SlotWindow> slots = AvailabilityPlanner.Plan(new DateOnly(2026, 11, 26), new TimeOnly(14, 0), new TimeOnly(16, 0), 20, Today, Istanbul, Now);

        slots.Count.ShouldBe(6);
        slots[0].StartsAt.ShouldBe(new DateTimeOffset(2026, 11, 26, 11, 0, 0, TimeSpan.Zero));
        slots[0].EndsAt.ShouldBe(new DateTimeOffset(2026, 11, 26, 11, 20, 0, TimeSpan.Zero));
        slots[5].StartsAt.ShouldBe(new DateTimeOffset(2026, 11, 26, 12, 40, 0, TimeSpan.Zero));
        slots[5].EndsAt.ShouldBe(new DateTimeOffset(2026, 11, 26, 13, 0, 0, TimeSpan.Zero));
        slots.ShouldAllBe(s => s.StartsAt.Offset == TimeSpan.Zero);
    }

    [Fact]
    public void Plan_ShortTail_IsDropped()
    {
        // Spec AC-1 örneği "14:00–14:50" kendi 15 dk adım kuralına uymuyor (plan 0016 A-2): kısa kuyruk 14:00–14:45 ile sınanır
        // (20 dk → 2 dilim, kalan 5 dk dilim olmaz); 14:50 bitişi adım hatasıdır.
        IReadOnlyList<SlotWindow> slots = AvailabilityPlanner.Plan(new DateOnly(2026, 11, 26), new TimeOnly(14, 0), new TimeOnly(14, 45), 20, Today, Istanbul, Now);

        slots.Count.ShouldBe(2);
        slots[^1].EndsAt.ShouldBe(new DateTimeOffset(2026, 11, 26, 11, 40, 0, TimeSpan.Zero));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            AvailabilityPlanner.Plan(new DateOnly(2026, 11, 26), new TimeOnly(14, 0), new TimeOnly(14, 50), 20, Today, Istanbul, Now));
    }

    [Fact]
    public void Plan_WithoutTimeZone_UsesFixedTurkeyOffset()
    {
        IReadOnlyList<SlotWindow> withZone = AvailabilityPlanner.Plan(new DateOnly(2027, 1, 15), new TimeOnly(9, 0), new TimeOnly(10, 0), 30, Today, Istanbul, Now);
        IReadOnlyList<SlotWindow> fixedOffset = AvailabilityPlanner.Plan(new DateOnly(2027, 1, 15), new TimeOnly(9, 0), new TimeOnly(10, 0), 30, Today, null, Now);

        fixedOffset.ShouldBe(withZone);
        withZone[0].StartsAt.ShouldBe(new DateTimeOffset(2027, 1, 15, 6, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Plan_Today_DropsStartedSlotsKeepsFutureOnes()
    {
        // Türkiye 12:00; 11:30–13:00 / 30 dk: 11:30 ve 12:00 başlamış (12:00 = şimdi), yalnızca 12:30 kalır.
        IReadOnlyList<SlotWindow> slots = AvailabilityPlanner.Plan(Today, new TimeOnly(11, 30), new TimeOnly(13, 0), 30, Today, Istanbul, Now);

        slots.Count.ShouldBe(1);
        slots[0].StartsAt.ShouldBe(new DateTimeOffset(2026, 11, 20, 9, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Plan_TodayAllStarted_ReturnsEmpty()
    {
        AvailabilityPlanner.Plan(Today, new TimeOnly(10, 0), new TimeOnly(12, 0), 30, Today, Istanbul, Now).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(60, true)]
    [InlineData(61, false)]
    public void IsDayAllowed_TodayToSixtyDays(int offset, bool expected)
    {
        AvailabilityPlanner.IsDayAllowed(Today.AddDays(offset), Today).ShouldBe(expected);
    }

    [Theory]
    [InlineData(14, 0, true)]
    [InlineData(14, 15, true)]
    [InlineData(14, 10, false)]
    [InlineData(14, 50, false)]
    public void IsOnStep_FifteenMinuteStep(int hour, int minute, bool expected)
    {
        AvailabilityPlanner.IsOnStep(new TimeOnly(hour, minute)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(14, 0, 16, 0, true)]
    [InlineData(8, 0, 16, 0, true)]
    [InlineData(8, 0, 16, 15, false)]
    [InlineData(16, 0, 14, 0, false)]
    [InlineData(14, 0, 14, 0, false)]
    public void IsRangeAllowed_StartBeforeEndAtMostEightHours(int sh, int sm, int eh, int em, bool expected)
    {
        AvailabilityPlanner.IsRangeAllowed(new TimeOnly(sh, sm), new TimeOnly(eh, em)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(15, true)]
    [InlineData(20, true)]
    [InlineData(30, true)]
    [InlineData(10, false)]
    [InlineData(45, false)]
    public void IsDurationAllowed_FifteenTwentyThirty(int minutes, bool expected)
    {
        AvailabilityPlanner.IsDurationAllowed(minutes).ShouldBe(expected);
    }

    [Theory]
    [InlineData(-1, 14, 0, 16, 0, 20)]
    [InlineData(61, 14, 0, 16, 0, 20)]
    [InlineData(5, 16, 0, 14, 0, 20)]
    [InlineData(5, 14, 10, 16, 0, 20)]
    [InlineData(5, 8, 0, 16, 15, 20)]
    [InlineData(5, 14, 0, 16, 0, 25)]
    public void Plan_InvalidInput_Throws(int dayOffset, int sh, int sm, int eh, int em, int minutes)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            AvailabilityPlanner.Plan(Today.AddDays(dayOffset), new TimeOnly(sh, sm), new TimeOnly(eh, em), minutes, Today, Istanbul, Now));
    }

    [Fact]
    public void Overlaps_AdjacentDoesNotOverlap()
    {
        SlotWindow slot = new(new DateTimeOffset(2026, 11, 26, 11, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 26, 13, 0, 0, TimeSpan.Zero));

        slot.Overlaps(slot.EndsAt, slot.EndsAt.AddHours(1)).ShouldBeFalse();
        slot.Overlaps(slot.StartsAt.AddHours(-1), slot.StartsAt).ShouldBeFalse();
        slot.Overlaps(slot.EndsAt.AddMinutes(-15), slot.EndsAt.AddHours(1)).ShouldBeTrue();
        slot.Overlaps(slot.StartsAt.AddMinutes(20), slot.StartsAt.AddMinutes(40)).ShouldBeTrue();
    }

    [Fact]
    public void NoticeText_PlaceholdersFilledInSinglePass()
    {
        DateTimeOffset local = new(2026, 11, 26, 14, 20, 0, TimeSpan.FromHours(3));

        string text = AppointmentNoticeText.Render("Randevu — {teacher}, {day} {time}. Neden: {reason}", "Ayşe {day}", local, "{teacher} yok");

        text.ShouldBe("Randevu — Ayşe {day}, 26.11.2026 14:20. Neden: {teacher} yok");
    }
}
