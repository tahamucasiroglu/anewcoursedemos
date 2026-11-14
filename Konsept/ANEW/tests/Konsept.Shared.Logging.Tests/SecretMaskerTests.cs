using Konsept.Shared.Kernel.Security;
using Shouldly;

namespace Konsept.Shared.Logging.Tests;

/// <summary>
/// Maskeleme test tablosu (plan 0008, Req 9; H-2). Sonraki spec'ler yeni gizli biçim ya da alan eklerse buraya
/// satır ekler. Kasıtlı bozma: <c>SecretMasker.Mask</c> girdiyi aynen döndürecek şekilde değiştirilince bu testler
/// kırmızıya döner (plan 0008, Build evidence).
/// </summary>
public sealed class SecretMaskerTests
{
    /// <summary>Gets serbest metin tablosu: girdi, beklenen çıktı ve çıktıda asla geçmemesi gereken parça.</summary>
    public static TheoryData<string, string, string> FreeTextTable => new()
    {
        { "key: sk-proj-AbCdEf123456789xyz bitti", "key: sk-…xyz bitti", "AbCdEf123456789" },
        { "sk-ant-api03-AbCdEfGhIjKlMnOp-Q7a", "sk-ant-…Q7a", "api03-AbCdEfGhIjKlMnOp" },
        { "anahtar=sk-or-v1-0123456789abcdef", "anahtar=sk-or-…def", "v1-0123456789abc" },
        { "token eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.sig_part-123 geldi", "token [jwt] geldi", "eyJzdWIiOiIxIn0" },
        { "Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc", "Authorization: Bearer [jwt]", "eyJhbGciOiJIUzI1NiJ9" },
        { "Authorization: Bearer opaqueToken123456", "Authorization: Bearer ***", "opaqueToken123456" },
        { "Host=x;Password=gizli-sifre;Username=u", "Host=x;Password=***;Username=u", "gizli-sifre" },
        { "Server=db;PWD = s3cr3t;Database=k", "Server=db;PWD=***;Database=k", "s3cr3t" },
        { "Npgsql hatası: sk-ant-api03-ZZZZZZZZZZZZ-abc reddedildi", "Npgsql hatası: sk-ant-…abc reddedildi", "ZZZZZZZZZZZZ" },
    };

    /// <summary>Serbest metne gömülü her biçim maskelenir; gizli parça çıktıda geçmez.</summary>
    /// <param name="input">Girdi.</param>
    /// <param name="expected">Beklenen çıktı.</param>
    /// <param name="secretPart">Çıktıda geçmemesi gereken parça.</param>
    [Theory]
    [MemberData(nameof(FreeTextTable))]
    public void Masking_FreeTextTable_EveryRowMasked(string input, string expected, string secretPart)
    {
        var masked = SecretMasker.Mask(input);

        masked.ShouldBe(expected);
        masked.ShouldNotContain(secretPart);
    }

    /// <summary>Adlandırılmış gizli alanlar alan türüne göre maskelenir; şifre hiçbir biçimde görünmez.</summary>
    /// <param name="field">Alan adı.</param>
    /// <param name="value">Değer.</param>
    /// <param name="expected">Beklenen.</param>
    [Theory]
    [InlineData("Password", "hunter2", "***")]
    [InlineData("password", "hunter2", "***")]
    [InlineData("NewPassword", "hunter2", "***")]
    [InlineData("new_password", "hunter2", "***")]
    [InlineData("ApiKey", "sk-proj-AbCdEf123456789a3f", "sk-…a3f")]
    [InlineData("OpenAiApiKey", "abcdefghijklmnopa3f", "…a3f")]
    [InlineData("AccessToken", "opaque-access", "***")]
    [InlineData("RefreshToken", "opaque-refresh", "***")]
    [InlineData("PairingCode", "123456", "***")]
    [InlineData("PairingSecret", "very-secret", "***")]
    [InlineData("StreamToken", "stream-abc", "***")]
    [InlineData("ConnectionString", "Host=pi;Password=gizli;Username=u", "Host=pi;Password=***;Username=u")]
    public void MaskField_NamedSecret_MaskedByKind(string field, string value, string expected)
    {
        SecretMasker.IsSecretField(field).ShouldBeTrue();
        SecretMasker.MaskField(field, value).ShouldBe(expected);
    }

    /// <summary>Gizli olmayan alanlar gizli sayılmaz ama içlerindeki gizli biçimler yine maskelenir.</summary>
    [Fact]
    public void MaskField_OrdinaryField_OnlyFreeTextMasking()
    {
        SecretMasker.IsSecretField("MaxTokens").ShouldBeFalse();
        SecretMasker.IsSecretField("MachineName").ShouldBeFalse();
        SecretMasker.MaskField("Message", "düz metin").ShouldBe("düz metin");
        SecretMasker.MaskField("Message", "key sk-proj-AbCdEf123456789xyz").ShouldBe("key sk-…xyz");
    }

    /// <summary>Çok kısa key'in uzunluğu bile sızmaz; tamamen gizlenir.</summary>
    [Fact]
    public void MaskApiKey_TooShort_FullyHidden() => SecretMasker.MaskApiKey("sk-abc").ShouldBe("***");
}
