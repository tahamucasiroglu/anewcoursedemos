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
        { "Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc", "Authorization: ***", "eyJhbGciOiJIUzI1NiJ9" },
        { "Header Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc", "Header Bearer [jwt]", "eyJhbGciOiJIUzI1NiJ9" },
        { "Authorization: Bearer opaqueToken123456", "Authorization: ***", "opaqueToken123456" },
        { "sent with Bearer opaqueToken123456 ok", "sent with Bearer *** ok", "opaqueToken123456" },
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
        { "Host=pi;Password=a,b=c;Username=u", "Host=pi;Password=***;Username=u", "b=c" },
        { "Host=pi;Password=ab&cd=ef;Username=u", "Host=pi;Password=***;Username=u", "cd=ef" },
        { "Host=pi;Password=ab }cd;Username=u", "Host=pi;Password=***;Username=u", "}cd" },
        { "Host=pi;Password=abc};Username=u", "Host=pi;Password=***;Username=u", "abc}" },
        { "Host=pi;Password=\"a\"\"b;c\";Username=u", "Host=pi;Password=\"***\";Username=u", "b;c" },
        { "cmd --password=hunter2 --verbose", "cmd --password=***", "hunter2" },
        { "--api-key=abcdefghijklmnop", "--api-key=…nop", "abcdefghijklm" },
        { "_password=hunter2", "_password=***", "hunter2" },
        { "2fa_secret=abc", "2fa_secret=***", "abc" },
        { "ÖzelToken=abc123", "ÖzelToken=***", "abc123" },
        { "{\\\"password\\\":\\\"hunter2\\\"}", "{\\\"password\\\":\\\"***\\\"}", "hunter2" },
        { "ApiKey=sk-proj-AbCdEf123456789xyz baglanti koptu", "ApiKey=sk-…xyz baglanti koptu", "AbCdEf123456789" },
        { "token:\nsonraki satır", "token:\nsonraki satır", "\u0000" },

        // Hunt (fix-3): URL kullanıcı bilgisi, başlıklar, CLI, YAML, XML, kaçış derinliği, URL kodlama, Türkçe adlar…
        { "connect failed: postgres://app:Hunter2Pass@db.local:5432/konsept", "connect failed: postgres://app:***@db.local:5432/konsept", "Hunter2Pass" },
        { "push https://Hunter2Pass@github.com/org/repo.git", "push https://***@github.com/org/repo.git", "Hunter2Pass" },
        { "redis://:s3cretpw@localhost:6379", "redis://:***@localhost:6379", "s3cretpw" },
        { "Authorization: Basic YWRtaW46SHVudGVyMlBhc3M=", "Authorization: ***", "YWRtaW46" },
        { "Cookie: .AspNetCore.Session=abc123def", "Cookie: ***", "abc123def" },
        { "app --password Hunter2Pass --verbose", "app --password *** --verbose", "Hunter2Pass" },
        { "Invoke-Sql -Password Hunter2Pass", "Invoke-Sql -Password ***", "Hunter2Pass" },
        { "password: |\n  Hunter2Pass\nuser: a", "password: |\n  ***\nuser: a", "Hunter2Pass" },
        { "db:\n  password:\n    hunter2\n  user: a", "db:\n  password:\n    ***\n  user: a", "hunter2" },
        { "\"credentials\": [\n  \"hunter2secret\"\n]", "\"credentials\": ***\n  ***\n]", "hunter2secret" },
        { "<password>Hunter2Pass</password>", "<password>***</password>", "Hunter2Pass" },
        { "<Password>\n  Hunter2Pass\n</Password>", "<Password>***\n</Password>", "Hunter2Pass" },
        { "body={\\\"password\\\":\\\"ab\\\\\\\"Hunter2Pass\\\"}", "body={\\\"password\\\":\\\"***\\\"}", "Hunter2Pass" },
        { "{\\\\\\\"password\\\\\\\":\\\\\\\"Hunter2Pass\\\\\\\"}", "{\\\\\\\"password\\\\\\\":\\\\\\\"***\\\\\\\"}", "Hunter2Pass" },
        { "Login failed: Password=Hun;ter2Pass (user a)", "Login failed: Password=***", "ter2Pass" },
        { "GET /login?user=a&password%3DHunter2Pass x", "GET /login?user=a&password%3D*** x", "Hunter2Pass" },
        { "redirect=%2Fcb%3Fuser%3Da%26password%3DHunter2Pass", "redirect=%2Fcb%3Fuser%3Da%26password%3D***", "Hunter2Pass" },
        { "DefaultEndpointsProtocol=https;AccountName=x;AccountKey=hunter2XYZ==;EndpointSuffix=core.windows.net", "DefaultEndpointsProtocol=https;AccountName=x;AccountKey=***;EndpointSuffix=core.windows.net", "hunter2XYZ" },
        { "AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI", "AWS_SECRET_ACCESS_KEY=***", "wJalrXUtnFEMI" },
        { "Jwt:SigningKey=abcdef0123456789", "Jwt:SigningKey=***", "abcdef0123456789" },
        { "şifre=hunter2", "şifre=***", "hunter2" },
        { "Parola: gizli1", "Parola: ***", "gizli1" },
        { "ŞİFRE=xyz123", "ŞİFRE=***", "xyz123" },
        { "password =hunter2", "password =***", "hunter2" },
        { "password​=hunter2", "password​=***", "hunter2" },
        { "password＝hunter2", "password＝***", "hunter2" },
        { "KONSEPT_INTEGRATIONS__OPENROUTER__FALLBACK__SECONDARY__ACCOUNT__PASSWORD=hunter2", "KONSEPT_INTEGRATIONS__OPENROUTER__FALLBACK__SECONDARY__ACCOUNT__PASSWORD=***", "hunter2" },
        { "secrets: topsecret1", "secrets: ***", "topsecret1" },
        { "passwords=pw1,pw2", "passwords=***", "pw2" },
        { "KV [\"[Password, p-val-kvlist]\"]", "KV [\"[Password, ***]\"]", "p-val-kvlist" },
        { "Bearer abcdefgh$ijkl@mn ve Bearer abc1234", "Bearer *** ve Bearer ***", "ijkl@mn" },
        { "Hata: bağlantı koptu (https://pi:8001/)", "Hata: bağlantı koptu (https://pi:8001/)", "\u0000" },

        // Hunt r2 (fix-3): boş değer sonrası sır, XML ad alanı/CDATA, URL listesi, YAML blok başlığı, SQL, Türkçe ekler…
        { "{\"password\":\"\",\"token\":\"S3CRETtok\"}", "{\"password\":\"\",\"token\":\"***\"}", "S3CRETtok" },
        { "Password=;Token=xS3CRET", "Password=;Token=***", "xS3CRET" },
        { "password=&token=xS3CRET", "password=&token=***", "xS3CRET" },
        { "<wsse:Password Type=\"PasswordText\">Hunter2Pass</wsse:Password>", "<wsse:Password Type=\"PasswordText\">***</wsse:Password>", "Hunter2Pass" },
        { "<password><![CDATA[Hunter2Pass]]></password>", "<password>***</password>", "Hunter2Pass" },
        { "redis://a:6379,redis://:pw2S3CRET@b", "redis://a:6379,redis://:***@b", "pw2S3CRET" },
        { "amqp://guest:gu#S3CRET@rabbit", "amqp://guest:***@rabbit", "S3CRET" },
        { "password: |  # yorum\n  Hunter2Pass\nuser: a", "password: |  # yorum\n  ***\nuser: a", "Hunter2Pass" },
        { "password: |2-\n  Hunter2Pass\nuser: a", "password: |2-\n  ***\nuser: a", "Hunter2Pass" },
        { "app --password \"Hunter 2Pass\" --verbose", "app --password \"***\" --verbose", "2Pass" },
        { "q=%7B%22password%22%3A%22Hunter2Pass%22%7D", "q=%7B%22password%22%3A%22***%22%7D", "Hunter2Pass" },
        { "ALTER ROLE app PASSWORD 'Hunter2Pass'", "ALTER ROLE app PASSWORD '***'", "Hunter2Pass" },
        { "passphrase=Hunter2Pass", "passphrase=***", "Hunter2Pass" },
        { "password_confirmation=Hunter2Pass", "password_confirmation=***", "Hunter2Pass" },
        { "Şifreniz: Hunter2Pass", "Şifreniz: ***", "Hunter2Pass" },
        { "şifrem=Hunter2Pass", "şifrem=***", "Hunter2Pass" },
        { "{\\u0022password\\u0022:\\u0022Hunter2Pass\\u0022}", "{\\u0022password\\u0022:\\u0022***\\u0022}", "Hunter2Pass" },
        { "hata\\nsk-proj-AbCdEf123456789xyz", "hata\\nsk-…xyz", "AbCdEf123456789" },
        { "GET /maps?key=AIzaSyA1234567890abcdefghijklmnopqrstuv x", "GET /maps?key=AIza…tuv x", "1234567890abcdef" },
        { "Ai:ApiKeys:Gemini=AIzaSyA1234567890abcdefghijklmnopqrstuv", "Ai:ApiKeys:…tuv", "1234567890abcdef" },
        { "gemini hata: AIzaSyA1234567890abcdefghijklmnopqrstuv geçersiz", "gemini hata: AIza…tuv geçersiz", "1234567890abcdef" },
        { "Ai:ApiKeys:Gemini=plainsecret12345", "Ai:ApiKeys:…345", "plainsecret" },
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

    /// <summary>
    /// Değerin içindeki uzun boşluk/sekme dizisi de doğrusal işlenir (review round 3, M-1): 64 KB'lık girdi 1 sn'nin çok
    /// altında; değer tamamen maskelenir.
    /// </summary>
    /// <param name="key">Anahtar.</param>
    /// <param name="fill">Dolgu karakteri.</param>
    [Theory]
    [InlineData("password", ' ')]
    [InlineData("token", '\t')]
    public void Mask_LongWhitespaceRunInValue_StaysFastAndMasked(string key, char fill)
    {
        var text = $"{key}=a" + new string(fill, SecretMasker.MaxInputLength - 20) + "b";
        SecretMasker.Mask(text);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var masked = SecretMasker.Mask(text);
        watch.Stop();

        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
        masked.ShouldBe($"{key}=***");
    }

    /// <summary>Üst sınırdan uzun metin kırpılır ve işaretlenir.</summary>
    [Fact]
    public void Mask_OverMaxLength_TruncatedWithMarker()
    {
        var masked = SecretMasker.Mask(new string('a', SecretMasker.MaxInputLength + 5000));

        // Boşluk yoksa kesim 256 karakter geri çekilir: sınırdaki yarım token bile çıkmaz.
        masked.Length.ShouldBe(SecretMasker.MaxInputLength - 256 + SecretMasker.TruncatedMarker.Length);
        masked.ShouldEndWith(SecretMasker.TruncatedMarker);
    }

    /// <summary>Kesim sınırına denk gelen JWT/URL yarım bırakılmaz; son boşlukta kesilir (hunt r2).</summary>
    [Fact]
    public void Mask_OverMaxLength_CutAtWhitespaceBeforePartialSecret()
    {
        var head = new string('a', SecretMasker.MaxInputLength - 100);
        var masked = SecretMasker.Mask($"{head} eyJ{new string('x', 300)}.yyy.zzz postgres://u:Hunter2Pass@h");

        masked.ShouldBe(head + SecretMasker.TruncatedMarker);
        masked.ShouldNotContain("eyJ");
    }

    /// <summary>Çok kısa key'in uzunluğu bile sızmaz; tamamen gizlenir.</summary>
    [Fact]
    public void MaskApiKey_TooShort_FullyHidden() => SecretMasker.MaskApiKey("sk-abc").ShouldBe("***");
}
