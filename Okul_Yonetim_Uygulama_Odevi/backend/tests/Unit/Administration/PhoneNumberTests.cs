using OkulYonetim.Domain.Identity;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Administration;

/// <summary>Telefon kuralı (spec 0005, Ö-3): Türkiye numarası farklı yazımlarla kabul edilir, tek biçimde saklanır; boş geçerli (isteğe bağlı).</summary>
public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("0532 123 45 67", "+905321234567")]
    [InlineData("5321234567", "+905321234567")]
    [InlineData("+90 532 123 45 67", "+905321234567")]
    [InlineData("905321234567", "+905321234567")]
    [InlineData("(0212) 555-44-33", "+902125554433")]
    [InlineData("0216.555.44.33", "+902165554433")]
    public void TryNormalize_ValidTurkishNumbers_ReturnsSingleFormat(string input, string expected)
    {
        PhoneNumber.TryNormalize(input, out string? normalized).ShouldBeTrue();
        normalized.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_Empty_IsValidAndNull(string? input)
    {
        PhoneNumber.TryNormalize(input, out string? normalized).ShouldBeTrue();
        normalized.ShouldBeNull();
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("0632 123 45 67")]
    [InlineData("0532 123 45 6")]
    [InlineData("+1 202 555 0101")]
    [InlineData("0532-abc-45-67")]
    [InlineData("05321234567890")]
    public void TryNormalize_Invalid_ReturnsFalse(string input) =>
        PhoneNumber.TryNormalize(input, out _).ShouldBeFalse();
}
