using OkulYonetim.Domain.Appointments;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Appointments;

/// <summary>
/// Spec 0016 AC-7/AC-8 (durum geçişleri): onay bekleyen talep başlamadan onaylanır ya da (nedenli/nedensiz) reddedilir; yanıtlanmış
/// talep yeniden yanıtlanamaz; başlamış talep yanıtlanamaz ve "Süresi doldu" görünür (başlangıç = şimdi başlamış sayılır); veli onay
/// bekleyen ya da onaylı randevuyu başlamadan iptal eder; öğretmen yalnızca onaylıyı iptal eder; konu ve neden sınırları (300).
/// </summary>
public sealed class AppointmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Start = new(2026, 11, 26, 11, 20, 0, TimeSpan.Zero);

    private static AvailabilitySlot Slot() =>
        AvailabilitySlot.Open(Guid.NewGuid(), Guid.NewGuid(), new SlotWindow(Start, Start.AddMinutes(20)), Now);

    private static Appointment Pending(AvailabilitySlot? slot = null) => Appointment.Request(slot ?? Slot(), Guid.NewGuid(), Guid.NewGuid(), "  Ali'nin matematik notları  ", Now);

    [Fact]
    public void Request_CopiesSlotTimesAndTrimsTopic()
    {
        AvailabilitySlot slot = Slot();
        Appointment appointment = Pending(slot);

        appointment.Status.ShouldBe(AppointmentStatus.Pending);
        appointment.SlotId.ShouldBe(slot.Id);
        appointment.TeacherUserId.ShouldBe(slot.TeacherUserId);
        appointment.StartsAt.ShouldBe(slot.StartsAt);
        appointment.EndsAt.ShouldBe(slot.EndsAt);
        appointment.Topic.ShouldBe("Ali'nin matematik notları");
        appointment.IsOpenAt(Now).ShouldBeTrue();
    }

    [Fact]
    public void Request_StartedOrRemovedSlotOrBadTopic_Throws()
    {
        AvailabilitySlot slot = Slot();
        Should.Throw<ArgumentOutOfRangeException>(() => Appointment.Request(slot, Guid.NewGuid(), Guid.NewGuid(), "konu", Start));
        Should.Throw<ArgumentOutOfRangeException>(() => Appointment.Request(slot, Guid.NewGuid(), Guid.NewGuid(), "   ", Now));
        Should.Throw<ArgumentOutOfRangeException>(() => Appointment.Request(slot, Guid.NewGuid(), Guid.NewGuid(), new string('a', 301), Now));
        Appointment.Request(slot, Guid.NewGuid(), Guid.NewGuid(), new string('a', 300), Now).Topic.Length.ShouldBe(300);
        slot.Remove(Now);
        Should.Throw<ArgumentOutOfRangeException>(() => Appointment.Request(slot, Guid.NewGuid(), Guid.NewGuid(), "konu", Now));
    }

    [Fact]
    public void Approve_Pending_BecomesApprovedOnce()
    {
        Appointment appointment = Pending();

        appointment.Approve(Now);

        appointment.Status.ShouldBe(AppointmentStatus.Approved);
        appointment.RespondedAt.ShouldBe(Now);
        appointment.CanRespond(Now).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => appointment.Approve(Now));
        Should.Throw<InvalidOperationException>(() => appointment.Reject(null, Now));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData(" Toplantım var ", "Toplantım var")]
    public void Reject_WithOrWithoutReason(string? reason, string? stored)
    {
        Appointment appointment = Pending();

        appointment.Reject(reason, Now);

        appointment.Status.ShouldBe(AppointmentStatus.Rejected);
        appointment.Reason.ShouldBe(stored);
        appointment.IsOpenAt(Now).ShouldBeFalse();
    }

    [Fact]
    public void Reject_ReasonOver300_Throws()
    {
        Should.Throw<InvalidOperationException>(() => Pending().Reject(new string('n', 301), Now));
        Appointment.IsReasonValid(new string('n', 300)).ShouldBeTrue();
    }

    [Fact]
    public void Started_PendingCannotBeAnsweredAndShowsExpired()
    {
        Appointment appointment = Pending();

        appointment.StateAt(Start.AddMinutes(-1)).ShouldBe(AppointmentState.Pending);
        appointment.StateAt(Start).ShouldBe(AppointmentState.Expired);
        appointment.CanRespond(Start).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => appointment.Approve(Start));
        Should.Throw<InvalidOperationException>(() => appointment.CancelByGuardian(Start));
        appointment.IsOpenAt(Start).ShouldBeFalse();
    }

    [Fact]
    public void Approved_StaysApprovedAfterStart()
    {
        Appointment appointment = Pending();
        appointment.Approve(Now);

        appointment.StateAt(Start.AddHours(1)).ShouldBe(AppointmentState.Approved);
        appointment.CanCancelByTeacher(Start).ShouldBeFalse();
        appointment.CanCancelByGuardian(Start).ShouldBeFalse();
    }

    [Fact]
    public void CancelByGuardian_PendingOrApproved_BeforeStart()
    {
        Appointment pending = Pending();
        Appointment approved = Pending();
        approved.Approve(Now);

        pending.CancelByGuardian(Now);
        approved.CancelByGuardian(Now);

        pending.Status.ShouldBe(AppointmentStatus.CancelledByGuardian);
        approved.Status.ShouldBe(AppointmentStatus.CancelledByGuardian);
        approved.CancelledAt.ShouldBe(Now);
        Should.Throw<InvalidOperationException>(() => approved.CancelByGuardian(Now));
    }

    [Fact]
    public void CancelByTeacher_OnlyApprovedBeforeStart()
    {
        Appointment pending = Pending();
        Appointment approved = Pending();
        approved.Approve(Now);

        Should.Throw<InvalidOperationException>(() => pending.CancelByTeacher(null, Now));
        approved.CancelByTeacher(" Hastayım ", Now);

        approved.Status.ShouldBe(AppointmentStatus.CancelledByTeacher);
        approved.Reason.ShouldBe("Hastayım");
        approved.StateAt(Now).ShouldBe(AppointmentState.CancelledByTeacher);
    }

    [Fact]
    public void Slot_RemoveAndStarted()
    {
        AvailabilitySlot slot = Slot();

        slot.HasStarted(Start.AddTicks(-1)).ShouldBeFalse();
        slot.HasStarted(Start).ShouldBeTrue();
        slot.Remove(Now);
        slot.RemovedAt.ShouldBe(Now);
        Should.Throw<ArgumentOutOfRangeException>(() => AvailabilitySlot.Open(Guid.NewGuid(), Guid.NewGuid(), new SlotWindow(Now, Now.AddMinutes(20)), Now));
    }
}
