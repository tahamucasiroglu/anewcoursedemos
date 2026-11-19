using OkulYonetim.Application.Features.Identity;
using OkulYonetim.Domain.Identity;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Identity;

/// <summary>Şifre kuralı ve e-posta karşılaştırma biçimi (spec 0004 AC-1, AC-3): en az 8 karakter; e-posta boşluk/harf farkından bağımsız.</summary>
public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("1234567", false)]
    [InlineData("12345678", true)]
    [InlineData("çok-uzun-ve-türkçe-şifre", true)]
    public void IsSatisfiedBy_Length_ReturnsExpected(string? password, bool expected) =>
        PasswordPolicy.IsSatisfiedBy(password).ShouldBe(expected);

    [Theory]
    [InlineData("  Ali@Ornek.TEST ", "ali@ornek.test")]
    [InlineData("	VELI.Kaya@Okul.TEST	", "veli.kaya@okul.test")]
    public void Normalize_TrimsAndLowersInvariant(string input, string expected) =>
        EmailAddress.Normalize(input).ShouldBe(expected);
}
