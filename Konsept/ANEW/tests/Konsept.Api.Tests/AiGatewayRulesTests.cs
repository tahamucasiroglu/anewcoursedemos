using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Application.Validators;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.AiGateway.Domain.Rules;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// AiGateway'in saf kuralları (spec 0004 Req 8–9): key biçimi (kırpma, 8–512, kontrol karakteri, önek aranmaz) ve maske
/// (ilk en fazla 3 + son 3; baştan karakter ancak 6 karakter gizli kalabiliyorsa).
/// </summary>
public sealed class AiGatewayRulesTests
{
    /// <summary>Biçim kuralı ve Türkçe mesajı; mesaj girilen değeri içermez.</summary>
    /// <param name="value">Girilen değer.</param>
    /// <param name="expected">Beklenen sorun.</param>
    [Theory]
    [InlineData(null, ApiKeyFormatIssue.Empty)]
    [InlineData("", ApiKeyFormatIssue.Empty)]
    [InlineData("    ", ApiKeyFormatIssue.Empty)]
    [InlineData("1234567", ApiKeyFormatIssue.TooShort)]
    [InlineData("  1234567  ", ApiKeyFormatIssue.TooShort)]
    [InlineData("12345678", ApiKeyFormatIssue.None)]
    [InlineData("  abcdefgh12  ", ApiKeyFormatIssue.None)]
    [InlineData("abc\u0001defgh", ApiKeyFormatIssue.ControlCharacter)]
    [InlineData("abcd\tefgh", ApiKeyFormatIssue.ControlCharacter)]
    [InlineData("anahtar-onek-yok-ama-gecerli", ApiKeyFormatIssue.None)]
    public void ApiKeyFormat_Rules(string? value, ApiKeyFormatIssue expected)
    {
        ApiKeyFormat.Check(value).ShouldBe(expected);
        var validation = new SaveApiKeyRequestValidator().Validate(new SaveApiKeyRequest(value));
        validation.IsValid.ShouldBe(expected == ApiKeyFormatIssue.None);
        if (!string.IsNullOrWhiteSpace(value))
        {
            validation.Errors.ShouldAllBe(e => !e.ErrorMessage.Contains(value.Trim(), StringComparison.Ordinal));
        }
    }

    /// <summary>512 sınırı: 512 geçer, 513 reddedilir.</summary>
    [Fact]
    public void ApiKeyFormat_LengthBoundary()
    {
        ApiKeyFormat.Check(new string('a', 512)).ShouldBe(ApiKeyFormatIssue.None);
        ApiKeyFormat.Check(new string('a', 513)).ShouldBe(ApiKeyFormatIssue.TooLong);
        new SaveApiKeyRequestValidator().Validate(new SaveApiKeyRequest(new string('a', 513))).Errors
            .ShouldHaveSingleItem().ErrorMessage.ShouldBe("API key en fazla 512 karakter olabilir.");
    }

    /// <summary>Maske: ilk en fazla 3 + "…" + son 3; kısa key'de baştan daha az; maske 8 karakterlik parça içermez.</summary>
    /// <param name="key">Key.</param>
    /// <param name="expected">Maske.</param>
    [Theory]
    [InlineData("sk-ant-api03-abcdefa3f", "sk-…a3f")]
    [InlineData("sk-proj-1234567890xyz", "sk-…xyz")]
    [InlineData("12345678", "…678")]
    [InlineData("1234567890", "1…890")]
    [InlineData("123456789012", "123…012")]
    public void ApiKeyMask_FirstUpTo3_Last3(string key, string expected)
    {
        ApiKeyMask.Mask(key).ShouldBe(expected);
        var visible = ApiKeyMask.Mask(key).Replace("…", string.Empty, StringComparison.Ordinal).Length;
        (key.Length - visible).ShouldBeGreaterThanOrEqualTo(Math.Min(6, key.Length - 3));
    }
}
