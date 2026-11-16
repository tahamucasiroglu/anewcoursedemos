using System.Security.Cryptography;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;
using Shouldly;

namespace Konsept.Modules.Identity.Tests;

/// <summary>
/// Eşleştirme kuralları, veritabanısız (spec 0012 AC-1, AC-3, AC-4, AC-10 kural düzeyi): kod biçimi ve alfabesi, 90 günlük
/// kayan ajan oturumu, kodun 10 dakika ve tek kullanımlık olması.
/// </summary>
public sealed class AgentPairingRulesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Kod <c>ABCD-1234</c> biçiminde, 8 karakterdir ve O/0/I/1 içermez; yazım farkları kanonik koda döner.</summary>
    [Fact]
    public void PairingCode_Generate_FormatAndAlphabet()
    {
        for (var i = 0; i < 500; i++)
        {
            var code = PairingCode.Generate(RandomNumberGenerator.GetInt32);

            code.ShouldMatch("^[A-Z2-9]{4}-[A-Z2-9]{4}$");
            code.ShouldNotContain('O');
            code.ShouldNotContain('0');
            code.ShouldNotContain('I');
            code.ShouldNotContain('1');
        }

        PairingCode.Normalize(" abcd-2345 ").ShouldBe("ABCD-2345");
        PairingCode.Normalize("abcd2345").ShouldBe("ABCD-2345");
        PairingCode.Normalize("ABCD-0345").ShouldBeNull();
        PairingCode.Normalize("ABC-2345").ShouldBeNull();
        PairingCode.Normalize(null).ShouldBeNull();
    }

    /// <summary>Ajan oturumu 90 gün kullanılmazsa geçersizdir; kullanıldıkça (son kullanım kaydırılınca) geçerli kalır.</summary>
    [Fact]
    public void AgentDevice_90Days_ExpiresAndTouchExtends()
    {
        var device = AgentDevice.Pair(Guid.NewGuid(), "TAHA-PC", "0.5.0", "hash", T0);

        device.IsActive(T0.AddDays(90)).ShouldBeTrue();
        device.IsActive(T0.AddDays(90).AddSeconds(1)).ShouldBeFalse();

        device.Touch(T0.AddDays(80));
        device.IsActive(T0.AddDays(169)).ShouldBeTrue();

        device.Revoke(T0.AddDays(81));
        device.IsActive(T0.AddDays(82)).ShouldBeFalse();
    }

    /// <summary>Kod 10 dakika geçerlidir ve tek kullanımlıktır: ikinci onay da ret de "kullanılmış" döner, belirteç bir kez verilir.</summary>
    [Fact]
    public void PairingRequest_SingleUseAndTenMinutes()
    {
        var expired = PairingRequest.Open("ABCD-2345", "h1", "PC", "0.5.0", T0);
        expired.CanDecide(T0.AddMinutes(10)).ShouldBe(PairingDecision.Expired);

        var request = PairingRequest.Open("ABCD-2346", "h2", "PC", "0.5.0", T0);
        request.BeginApproval(Guid.NewGuid(), T0.AddMinutes(9)).ShouldBe(PairingDecision.Accepted);
        request.BeginApproval(Guid.NewGuid(), T0.AddMinutes(9)).ShouldBe(PairingDecision.AlreadyUsed);
        request.Reject(T0.AddMinutes(9)).ShouldBe(PairingDecision.AlreadyUsed);
        request.TakeToken().ShouldBeNull();

        request.CompleteApproval("knr_token");
        request.TakeToken().ShouldBe("knr_token");
        request.TakeToken().ShouldBeNull();
        request.State.ShouldBe(PairingState.Delivered);
    }
}
