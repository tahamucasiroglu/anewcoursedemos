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
        { "Server=db;PWD = s3cr3t;Database=k", "Server=db;PWD = ***;Database=k", "s3cr3t" },
        { "Npgsql hatası: sk-ant-api03-ZZZZZZZZZZZZ-abc reddedildi", "Npgsql hatası: sk-ant-…abc reddedildi", "ZZZZZZZZZZZZ" },
        { "ChangePasswordRequest { CurrentPassword = old-pw-1, NewPassword = new-pw-2 }", "ChangePasswordRequest { CurrentPassword = ***, NewPassword = *** }", "pw-" },
        { "{\"user\":\"a\",\"password\":\"hunter2\"}", "{\"user\":\"a\",\"password\":\"***\"}", "hunter2" },
        { "password: hunter2", "password: ***", "hunter2" },
        { "Password='a b;c' sonra", "Password='***' sonra", "a b;c" },
        { "db_password=s3cret", "db_password=***", "s3cret" },
        { "PairingSecret = xyz-1, AccessToken = opaque-1", "PairingSecret = ***, AccessToken = ***", "opaque-1" },
        { "key_sk-proj-AbCdEf123456789xyz", "key_sk-…xyz", "AbCdEf123456789" },
        { "/cb?access_token=abc123def&x=1", "/cb?access_token=***&x=1", "abc123def" },
        { "ApiClientOptions { ServiceKey = 0123abcd4567ef }", "ApiClientOptions { ServiceKey = *** }", "0123abcd4567ef" },
        { "ApiKey = sk-proj-AbCdEf123456789a3f", "ApiKey = sk-…a3f", "AbCdEf123456789" },
        { "OpenAiApiKey: abcdefghijklmnopq9z", "OpenAiApiKey: …q9z", "abcdefghijklmnop" },
        { "System.InvalidOperationException: Password=exc-pw-1 reddedildi", "System.InvalidOperationException: Password=***", "exc-pw-1" },
        { "Req { NewPassword = hun ter2 }", "Req { NewPassword = *** }", "ter2" },
        { "Host=pi;Password=ab,cd&ef;Username=u", "Host=pi;Password=***;Username=u", "cd&ef" },
        { "{\"password\":\"pa\\\"ss word\"}", "{\"password\":\"***\"}", "ss word" },
        { "PairingCode = 123 456", "PairingCode = ***", "456" },
        { "Password := abc-def", "Password := ***", "abc-def" },
        { "NewPassword=\"unterminated secret", "NewPassword=\"***", "secret" },
        { "{\"password\": {\"value\":\"nested-pw\"}}", "{\"password\": ***", "nested-pw" },
        { "{\"password\":[\"arr-pw\"]}", "{\"password\":***", "arr-pw" },
        { "refresh_token=eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc", "refresh_token=***", "]" },
        { "ApiKey=abc…realsecret", "ApiKey=…ret", "realsec" },
        { "user_id=42, password=p4ss, role=admin", "user_id=42, password=***, role=admin", "p4ss" },
        { "Hata: bağlantı koptu (https://pi:8001/)", "Hata: bağlantı koptu (https://pi:8001/)", "\u0000" },
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
    [InlineData("ServiceKey", "0123abcd4567ef", "***")]
    [InlineData("Internal__ServiceKey", "0123abcd4567ef", "***")]
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

    /// <summary>
    /// Maskeleme girdi boyuyla doğrusal kalır: 32 KB'lık kötü niyetli metin (çok sayıda <c>a=a=</c> ve gizli anahtar)
    /// 1 sn'nin çok altında işlenir (review round 2, L-1).
    /// </summary>
    /// <param name="unit">Tekrarlanan parça.</param>
    [Theory]
    [InlineData("a=a=")]
    [InlineData("a:a:")]
    [InlineData("password=a ")]
    [InlineData("x, token: ")]
    public void Mask_LargeAdversarialInput_StaysFast(string unit)
    {
        var text = string.Concat(Enumerable.Repeat(unit, (32 * 1024) / unit.Length));
        SecretMasker.Mask(text);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        SecretMasker.Mask(text);
        watch.Stop();

        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    /// <summary>Çok kısa key'in uzunluğu bile sızmaz; tamamen gizlenir.</summary>
    [Fact]
    public void MaskApiKey_TooShort_FullyHidden() => SecretMasker.MaskApiKey("sk-abc").ShouldBe("***");
}
