using OkulYonetim.Domain.Identity;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Identity;

/// <summary>Kilit kuralı (spec 0004 AC-6): 3 hatada 15 dk kilit; kilit bitince sayaç baştan; başarı sıfırlar.</summary>
public sealed class LoginThrottleRulesTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegisterFailure_ThirdFailure_LocksFor15Minutes()
    {
        LoginThrottle throttle = LoginThrottle.For("a@ornek.test");

        throttle.RegisterFailure(Start).ShouldBeFalse();
        throttle.RegisterFailure(Start).ShouldBeFalse();
        throttle.RegisterFailure(Start).ShouldBeTrue();

        throttle.IsLocked(Start.AddMinutes(14).AddSeconds(59)).ShouldBeTrue();
        throttle.IsLocked(Start.AddMinutes(15)).ShouldBeFalse();
    }

    [Fact]
    public void RegisterFailure_AfterLockExpired_StartsCountingAgain()
    {
        LoginThrottle throttle = LoginThrottle.For("a@ornek.test");
        for (int i = 0; i < 3; i++)
        {
            throttle.RegisterFailure(Start);
        }

        throttle.RegisterFailure(Start.AddMinutes(16)).ShouldBeFalse();
        throttle.FailedCount.ShouldBe(1);
        throttle.IsLocked(Start.AddMinutes(16)).ShouldBeFalse();
    }

    [Fact]
    public void Reset_AfterTwoFailures_RequiresThreeNewFailuresToLock()
    {
        LoginThrottle throttle = LoginThrottle.For("a@ornek.test");
        throttle.RegisterFailure(Start);
        throttle.RegisterFailure(Start);

        throttle.Reset();

        throttle.RegisterFailure(Start).ShouldBeFalse();
        throttle.RegisterFailure(Start).ShouldBeFalse();
        throttle.IsLocked(Start).ShouldBeFalse();
    }

    [Fact]
    public void UserSession_IdleSevenDays_IsInactive_RevokedIsInactive()
    {
        UserSession session = UserSession.Open(Guid.NewGuid(), null, "ozet", Start, ip: null);

        session.IsActive(Start.AddDays(7).AddSeconds(-1)).ShouldBeTrue();
        session.IsActive(Start.AddDays(7)).ShouldBeFalse();
        session.Rotate("yeni-ozet", Start.AddDays(6));
        session.IsActive(Start.AddDays(12)).ShouldBeTrue();
        session.Revoke(Start.AddDays(6));
        session.IsActive(Start.AddDays(6)).ShouldBeFalse();
    }
}
