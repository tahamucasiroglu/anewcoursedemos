using System.Text;
using OkulYonetim.Infrastructure.Security;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Platform;

/// <summary>Spec 0007: RFC 6238 test vektörleri (SHA1, 6 hane), Base32, ±1 dilim.</summary>
public sealed class TotpTests
{
    private static readonly byte[] RfcKey = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    [InlineData(20000000000L, "353130")]
    public void Code_MatchesRfc6238Vectors(long unixSeconds, string expected) =>
        Totp.Code(RfcKey, unixSeconds / 30).ShouldBe(expected);

    [Fact]
    public void Base32_RoundTrips_AndSecretIs32Chars()
    {
        Totp totp = new();
        string secret = totp.GenerateSecret();
        secret.ShouldMatch("^[A-Z2-7]{32}$");
        Totp.ToBase32(Totp.FromBase32(secret)).ShouldBe(secret);
        Totp.ToBase32(RfcKey).ShouldBe("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ");
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(-2, false)]
    [InlineData(2, false)]
    public void Verify_AcceptsOnlyAdjacentSteps(int offset, bool accepted)
    {
        DateTimeOffset now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_015);
        string secret = Totp.ToBase32(RfcKey);
        string code = Totp.Code(RfcKey, (now.ToUnixTimeSeconds() / 30) + offset);

        new Totp().Verify(secret, code, now, out long step).ShouldBe(accepted);
        if (accepted)
        {
            step.ShouldBe((now.ToUnixTimeSeconds() / 30) + offset);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("abcdef")]
    [InlineData("1234567")]
    public void Verify_RejectsMalformedCodes(string code) =>
        new Totp().Verify(Totp.ToBase32(RfcKey), code, DateTimeOffset.UtcNow, out _).ShouldBeFalse();

    [Fact]
    public void ProvisioningUri_HasIssuerAndParameters()
    {
        string uri = new Totp().ProvisioningUri("ABC", "ayse@ornek.test");
        uri.ShouldStartWith("otpauth://totp/Okul%20Y%C3%B6netim:ayse%40ornek.test?secret=ABC");
        uri.ShouldContain("digits=6");
        uri.ShouldContain("period=30");
    }
}
