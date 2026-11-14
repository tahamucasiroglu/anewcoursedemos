using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Application.Validators;
using Shouldly;

namespace Konsept.Modules.Identity.Tests;

/// <summary>Kayıt ve giriş doğrulama kuralları (spec 0002 AC-3, AC-16 boş alan).</summary>
public sealed class ValidatorTests
{
    private const string ValidName = "Ayşe";
    private const string ValidEmail = "ayse@x.com";
    private const string ValidPassword = "sifre123";

    /// <summary>Gets sınır tablosu: alan, değer, geçerli mi, beklenen Türkçe mesaj (geçersizse).</summary>
    public static TheoryData<string, string, bool, string> BoundaryTable => new()
    {
        { "Name", "   ", false, "Adınızı girin." },
        { "Name", " " + new string('a', 101) + " ", false, "Ad en fazla 100 karakter olabilir." },
        { "Name", "a", true, string.Empty },
        { "Name", "  " + new string('a', 100) + "  ", true, string.Empty },
        { "Email", "ayse.x.com", false, "Geçerli bir e-posta adresi girin." },
        { "Email", "ayse@x", false, "Geçerli bir e-posta adresi girin." },
        { "Email", new string('a', 249) + "@x.com", false, "E-posta en fazla 254 karakter olabilir." },
        { "Email", new string('a', 248) + "@x.com", true, string.Empty },
        { "Password", "abc1234", false, "Şifre 8 ile 128 karakter arasında olmalı." },
        { "Password", "a" + new string('1', 128), false, "Şifre 8 ile 128 karakter arasında olmalı." },
        { "Password", "abcdefgh", false, "Şifre en az bir harf ve en az bir rakam içermeli." },
        { "Password", "12345678", false, "Şifre en az bir harf ve en az bir rakam içermeli." },
        { "Password", "abcdefg1", true, string.Empty },
        { "Password", "a" + new string('1', 127), true, string.Empty },
    };

    /// <summary>Her sınır değeri: geçersiz alan yalnızca kendi Türkçe mesajını verir; sınır içi değerler kabul edilir.</summary>
    /// <param name="field">Alan.</param>
    /// <param name="value">Değer.</param>
    /// <param name="valid">Geçerli mi.</param>
    /// <param name="message">Beklenen mesaj.</param>
    [Theory]
    [MemberData(nameof(BoundaryTable))]
    public void RegisterValidator_BoundaryTable(string field, string value, bool valid, string message)
    {
        var request = new RegisterRequest(
            field == "Name" ? value : ValidName,
            field == "Email" ? value : ValidEmail,
            field == "Password" ? value : ValidPassword);

        var result = new RegisterRequestValidator().Validate(request);

        result.IsValid.ShouldBe(valid);
        if (!valid)
        {
            result.Errors.ShouldAllBe(e => e.PropertyName == field);
            result.Errors.Select(e => e.ErrorMessage).ShouldContain(message);
        }
    }

    /// <summary>Boş e-posta ve şifreyle giriş alan hatası verir.</summary>
    [Fact]
    public void LoginValidator_EmptyFields_FieldErrors()
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest(" ", string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(e => e.PropertyName).Order(StringComparer.Ordinal).ShouldBe(["Email", "Password"]);
    }
}
