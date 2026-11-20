using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OkulYonetim.Infrastructure.Security;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Platform;

/// <summary>Spec 0007 AC-11: anahtar şifreli saklanır; anahtarsız yapılandırma "ayarlı değil"; bozulmuş veri çözülmez.</summary>
public sealed class AesGcmSecretProtectorTests
{
    private static AesGcmSecretProtector With(string? key) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["TwoFactor:EncryptionKey"] = key }).Build(), NullLogger<AesGcmSecretProtector>.Instance);

    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Fact]
    public void RoundTrips_AndCipherIsRandomizedAndOpaque()
    {
        AesGcmSecretProtector protector = With(NewKey());
        const string Secret = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";

        string first = protector.Protect(Secret);
        string second = protector.Protect(Secret);

        protector.IsConfigured.ShouldBeTrue();
        first.ShouldNotBe(second);
        first.ShouldNotContain(Secret);
        protector.Unprotect(first).ShouldBe(Secret);
    }

    [Fact]
    public void TamperedOrForeignCipher_DoesNotDecrypt()
    {
        AesGcmSecretProtector protector = With(NewKey());
        byte[] cipher = Convert.FromBase64String(protector.Protect("gizli"));
        cipher[^1] ^= 1;

        Should.Throw<CryptographicException>(() => protector.Unprotect(Convert.ToBase64String(cipher)));
        Should.Throw<CryptographicException>(() => With(NewKey()).Unprotect(protector.Protect("gizli")));
    }

    [Fact]
    public void MissingOrMalformedKey_IsNotConfigured()
    {
        With(null).IsConfigured.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => With(null).Protect("x"));
        With(Convert.ToBase64String(new byte[16])).IsConfigured.ShouldBeFalse();
        With("base64-olmayan-deger!").IsConfigured.ShouldBeFalse();
    }
}
