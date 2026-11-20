using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OkulYonetim.Infrastructure.Security;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Students;

/// <summary>
/// Spec 0009 AC-10: sağlık bilgisi şifreli saklanır (rastgele nonce, düz metin şifreli veride yok); anahtar iki adımlı
/// doğrulama anahtarından ayrıdır (biriyle şifrelenen öbürüyle çözülmez); anahtarsız ya da bozuk anahtarlı yapılandırma
/// "ayarlı değil" sayılır ve şifreleme yapılmaz.
/// </summary>
public sealed class AesGcmPersonalDataProtectorTests
{
    private static AesGcmPersonalDataProtector With(string? key, string? twoFactorKey = null) =>
        new(Config(key, twoFactorKey), NullLogger<AesGcmPersonalDataProtector>.Instance);

    private static IConfiguration Config(string? key, string? twoFactorKey) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PersonalData:EncryptionKey"] = key,
            ["TwoFactor:EncryptionKey"] = twoFactorKey,
        }).Build();

    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Fact]
    public void Protect_RoundTripsTurkishText_CipherIsRandomizedAndOpaque()
    {
        AesGcmPersonalDataProtector protector = With(NewKey());
        const string Allergy = "Fıstık ve çilek alerjisi; ilaç: Ventolin";

        string first = protector.Protect(Allergy);
        string second = protector.Protect(Allergy);

        protector.IsConfigured.ShouldBeTrue();
        first.ShouldNotBe(second);
        first.ShouldNotContain("Fıstık");
        first.ShouldNotContain("Ventolin");
        protector.Unprotect(first).ShouldBe(Allergy);
    }

    [Fact]
    public void Protect_UsesItsOwnKey_NotTheTwoFactorKey()
    {
        string personal = NewKey();
        string twoFactor = NewKey();
        AesGcmPersonalDataProtector protector = With(personal, twoFactor);
        AesGcmSecretProtector secrets = new(Config(personal, twoFactor), NullLogger<AesGcmSecretProtector>.Instance);

        string cipher = protector.Protect("astım");

        Should.Throw<CryptographicException>(() => secrets.Unprotect(cipher));
        With(null, personal).IsConfigured.ShouldBeFalse("iki adımlı doğrulama anahtarı kişisel veri anahtarı yerine geçmez");
    }

    [Fact]
    public void Unprotect_TamperedOrForeignCipher_Throws()
    {
        AesGcmPersonalDataProtector protector = With(NewKey());
        byte[] cipher = Convert.FromBase64String(protector.Protect("not"));
        cipher[^1] ^= 1;

        Should.Throw<CryptographicException>(() => protector.Unprotect(Convert.ToBase64String(cipher)));
        Should.Throw<CryptographicException>(() => With(NewKey()).Unprotect(protector.Protect("not")));
        Should.Throw<CryptographicException>(() => protector.Unprotect(Convert.ToBase64String(new byte[5])));
    }

    [Fact]
    public void IsConfigured_MissingOrMalformedKey_IsFalseAndDoesNotEncrypt()
    {
        With(null).IsConfigured.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => With(null).Protect("x"));
        With(Convert.ToBase64String(new byte[16])).IsConfigured.ShouldBeFalse();
        With("base64-olmayan-deger!").IsConfigured.ShouldBeFalse();
    }
}
