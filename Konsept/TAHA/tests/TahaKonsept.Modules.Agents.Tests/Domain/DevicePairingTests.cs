using Shouldly;
using TahaKonsept.Modules.Agents.Domain.Entities;
using TahaKonsept.Modules.Agents.Domain.Enums;
using TahaKonsept.Modules.Agents.Domain.Errors;
using Xunit;

namespace TahaKonsept.Modules.Agents.Tests.Domain;

/// <summary>
/// <see cref="DevicePairing"/> durum makinesi: bekleme → onay → token bir kez; süre dolması; çok sık yoklamada yavaşla; ret.
/// </summary>
public sealed class DevicePairingTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Yeni istek bekliyor; ilk yoklama "pending".</summary>
    [Fact]
    public void New_pairing_is_pending()
    {
        var pairing = Create();

        pairing.Status.ShouldBe(DevicePairingStatus.Pending);
        pairing.Poll(T0.AddSeconds(5)).ShouldBe(PairingPollOutcome.Pending);
    }

    /// <summary>Aralıktan önce yoklama "slow_down" verir ve aralığı 5 sn artırır.</summary>
    [Fact]
    public void Polling_too_fast_returns_slow_down_and_increases_interval()
    {
        var pairing = Create();
        pairing.Poll(T0.AddSeconds(5)).ShouldBe(PairingPollOutcome.Pending);

        pairing.Poll(T0.AddSeconds(7)).ShouldBe(PairingPollOutcome.SlowDown);
        pairing.IntervalSeconds.ShouldBe(10);

        pairing.Poll(T0.AddSeconds(17)).ShouldBe(PairingPollOutcome.Pending);
    }

    /// <summary>Onaylanınca yoklama "approved"; tüketildikten sonra tekrar yoklama süresi dolmuş sayılır (token bir kez).</summary>
    [Fact]
    public void Approved_pairing_yields_token_once()
    {
        var pairing = Create();
        var userId = Guid.NewGuid();

        pairing.Approve(userId, T0.AddSeconds(10)).IsSuccess.ShouldBeTrue();
        pairing.UserId.ShouldBe(userId);
        pairing.Poll(T0.AddSeconds(12)).ShouldBe(PairingPollOutcome.Approved);

        var deviceId = Guid.NewGuid();
        pairing.Consume(deviceId, T0.AddSeconds(12)).IsSuccess.ShouldBeTrue();
        pairing.AgentDeviceId.ShouldBe(deviceId);

        pairing.Poll(T0.AddSeconds(30)).ShouldBe(PairingPollOutcome.Expired);
        pairing.Consume(Guid.NewGuid(), T0.AddSeconds(30)).Error.ShouldBe(AgentErrors.ExpiredToken);
    }

    /// <summary>Süresi dolan istek "expired" döner ve onaylanamaz.</summary>
    [Fact]
    public void Expired_pairing_cannot_be_approved_or_polled()
    {
        var pairing = Create();
        var late = T0.AddMinutes(11);

        pairing.IsExpired(late).ShouldBeTrue();
        pairing.Poll(late).ShouldBe(PairingPollOutcome.Expired);
        pairing.Approve(Guid.NewGuid(), late).Error.ShouldBe(AgentErrors.UserCodeExpired);
    }

    /// <summary>Reddedilen istek "denied" döner; ikinci karar çakışma verir.</summary>
    [Fact]
    public void Denied_pairing_returns_denied()
    {
        var pairing = Create();

        pairing.Deny(Guid.NewGuid(), T0.AddSeconds(3)).IsSuccess.ShouldBeTrue();
        pairing.Poll(T0.AddSeconds(10)).ShouldBe(PairingPollOutcome.Denied);
        pairing.Approve(Guid.NewGuid(), T0.AddSeconds(11)).Error.ShouldBe(AgentErrors.PairingAlreadyCompleted);
    }

    /// <summary>Onaylanmadan tüketilemez.</summary>
    [Fact]
    public void Pending_pairing_cannot_be_consumed()
    {
        Create().Consume(Guid.NewGuid(), T0).IsFailure.ShouldBeTrue();
    }

    private static DevicePairing Create() =>
        DevicePairing.Start("hash", "ABCD2345", "TAHA-PC", "0.3.0", T0, TimeSpan.FromMinutes(10), 5);
}
