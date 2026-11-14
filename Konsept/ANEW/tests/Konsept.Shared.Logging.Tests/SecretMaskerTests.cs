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
        { "{\"password\": {\"value\":\"nested-pw\"}}", "{\"password\": ***}", "nested-pw" },
        { "{\"password\":[\"arr-pw\"]}", "{\"password\":***}", "arr-pw" },
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
        { "Ai:ApiKeys:Gemini=AIzaSyA1234567890abcdefghijklmnopqrstuv", "Ai:ApiKeys:Gemini=AIza…tuv", "1234567890abcdef" },
        { "gemini hata: AIzaSyA1234567890abcdefghijklmnopqrstuv geçersiz", "gemini hata: AIza…tuv geçersiz", "1234567890abcdef" },
        { "Ai:ApiKeys:Gemini=plainsecret12345", "Ai:ApiKeys:Gemini=…345", "plainsecret" },

        // Hunt r3 (fix-4): API key alanı, çerezler, kodlu anahtarlar (çözülmüş görünüm), anahtar süsleri, XML, URL, tekrar eden değer…
        { "api_key=abcd1234efgh5678 password=Hunter2Pass", "api_key=…678 password=***", "Hunter2Pass" },
        { "{\"apiKey\":\" sk_live_abcdefghijklmnop1234\"}", "{\"apiKey\":\"…234\"}", "abcdefghijklmnop" },
        { "\"ApiKeys\": { \"OpenRouter\": \"elv1234abcd5678efgh\", \"Other\": \"zzq9876yyyy5432\" }", "\"ApiKeys\": ***", "zzq9876yyyy" },
        { "Request headers: Cookie: theme=dark; session=CfDJ8SECRETSESSION", "Request headers: Cookie: ***", "CfDJ8SECRET" },
        { "{\"body\":\"\\u003CPassword\\u003EGizliSoap99\\u003C/Password\\u003E\"}", "{\"body\":\"\\u003CPassword\\u003E***\\u003C/Password\\u003E\"}", "GizliSoap99" },
        { "{\"\\u015Fifre\":\"Gizli1234\"}", "{\"\\u015Fifre\":\"***\"}", "Gizli1234" },
        { "kullanici=ali&%C5%9Fifre=Gizli1234&x=1", "kullanici=ali&%C5%9Fifre=***&x=1", "Gizli1234" },
        { "{\"url\":\"postgres:\\/\\/app:Hunter2Pass@db.local:5432\\/k\"}", "{\"url\":\"postgres:\\/\\/app:***@db.local:5432\\/k\"}", "Hunter2Pass" },
        { "{\"m\":\"{\\\\u0022password\\\\u0022:\\\\u0022Hunter2Pass\\\\u0022}\"}", "{\"m\":\"{\\\\u0022password\\\\u0022:\\\\u0022***\\\\u0022}\"}", "Hunter2Pass" },
        { "HttpRequestException: 401\\r\\nBearer opaqueTok-9f8e7d6c5b4a", "HttpRequestException: 401\\r\\nBearer ***", "opaqueTok" },
        { "q=%7B%22key%22%3A%22sk-or-v1-0123456789abcdef0123456789abcdef%22%7D", "q=%7B%22key%22%3A%22sk-or-…def%22%7D", "0123456789abcdef0123" },
        { "[Parameters=[@__token_0='TokValue12345' (Size = 4000)]", "[Parameters=[@__token_0='***' (Size = 4000)]", "TokValue12345" },
        { "user%5Bpassword%5D=NestedForm123&user%5Bname%5D=bob", "user%5Bpassword%5D=***", "NestedForm123" },
        { "Login {[\"UserName\"]=\"bob\", [\"Password\"]=\"Hunter2Secret\"}", "Login {[\"UserName\"]=\"bob\", [\"Password\"]=\"***\"}", "Hunter2Secret" },
        { "- **Password**: Hunter2Secret\n- **Token**: Tok2Secret99", "- **Password**: ***\n- **Token**: ***", "Tok2Secret99" },
        { "Ai__ApiKeys__ElevenLabs=zzq9876yyyy5432", "Ai__ApiKeys__ElevenLabs=…432", "zzq9876yyyy" },
        { "<ClientSecret Value=\"CsAttr12345\"/>", "<ClientSecret Value=\"***\"/>", "CsAttr12345" },
        { "<Credentials><Username>bob</Username><Value>Hunter2Pass</Value></Credentials>", "<Credentials>***</Credentials>", "Hunter2Pass" },
        { "client_secret= \"\"  # rotated, token=zzq9876yyyy5432", "client_secret= \"\"  # rotated, token=***", "zzq9876yyyy" },
        { "postgres://user:Secr;etPassXY@host:5432/db", "postgres://user:***@host:5432/db", "etPassXY" },
        { "{\"password\":\"Hunter2Pass\",\"note\":\"retry with Hunter2Pass\"}", "{\"password\":\"***\",\"note\":\"retry with ***\"}", "Hunter2Pass" },
        { "(Password, TupleSecret99)", "(Password, ***)", "TupleSecret99" },
        { "Invalid API key: zzq9876yyyy5432", "Invalid API key: …432", "zzq9876yyyy" },
        { "Pairing code: 482913", "Pairing code: ***", "482913" },
        { "Erişim anahtarı=zzq9876yyyy5432", "Erişim anahtarı=***", "zzq9876yyyy" },
        { "Token'ınız: Gizli12345", "Token'ınız: ***", "Gizli12345" },
        { "CREATE ROLE app LOGIN PASSWORD E'Hunter2Secret';", "CREATE ROLE app LOGIN PASSWORD E'***';", "Hunter2Secret" },
        { "db:\n    password: |\n\t\tHunter2Pass\n", "db:\n    password: |\n\t\t***\n", "Hunter2Pass" },
        { "MaxTokens=4096 InputTokens=12 Invalid key format", "MaxTokens=4096 InputTokens=12 Invalid key format", "\u0000" },

        // Hunt r4 / review r4 (fix-5): yarım vekil çift, gizli ad + sözcük, spec'teki Türkçe adlar, hasar korumaları…
        { "password=\uD800abcdefg", "password=***", "abcdefg" },
        { "token=\\ud83dabcdef12", "token=***", "abcdef12" },
        { "PairingService: Secret key: wJalrXUtnFEMI2K7MDENG", "PairingService: Secret key: ***", "wJalrXUtnFEMI" },
        { "Şifre değeri: Gizli12345", "Şifre değeri: ***", "Gizli12345" },
        { "Eşleştirme onay sırrı: Qw7Er8Ty9Ui0", "Eşleştirme onay sırrı: ***", "Qw7Er8Ty9Ui0" },
        { "Ajan oturum belirteci: Zm9vYmFyYmF6cXV4MTIz", "Ajan oturum belirteci: ***", "Zm9vYmFy" },
        { "İç uç reddedildi; gelen servis anahtarı=7f3a9c2e1b8d4f6a", "İç uç reddedildi; gelen servis anahtarı=***", "7f3a9c2e" },
        { "{\"pairCode\":\"482913\",\"deviceName\":\"pc\"}", "{\"pairCode\":\"***\",\"deviceName\":\"pc\"}", "482913" },
        { "DB_PASS=Hunter2Pass99", "DB_PASS=***", "Hunter2Pass99" },
        { "{\"title\":\"Validation failed.\",\"errors\":{\"Password\":[\"The Password field is required.\"]},\"traceId\":\"00-abc\"}", "{\"title\":\"Validation failed.\",\"errors\":{\"Password\":***},\"traceId\":\"00-abc\"}", "field is required" },
        { "Microsoft.AspNetCore.Authorization.DefaultAuthorizationService: Authorization failed. These requirements were not met", "Microsoft.AspNetCore.Authorization.DefaultAuthorizationService: Authorization failed. These requirements were not met", "\u0000" },
        { "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationHandler: AuthenticationScheme: Cookies signed in.", "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationHandler: AuthenticationScheme: Cookies signed in.", "\u0000" },
        { "İşlem sırasında sıra numarası 5 bypass=true", "İşlem sırasında sıra numarası 5 bypass=true", "\u0000" },

        // Hunt r5 (fix-6): sıradan log cümleleri bozulmaz; yol ve SQL parametresi değer sayılmaz; port şifre değildir.
        { "Token expired: user=alice at 2026-11-14T10:00:00Z", "Token expired: user=alice at 2026-11-14T10:00:00Z", "\u0000" },
        { "Password reset: user=x requested", "Password reset: user=x requested", "\u0000" },
        { "Belirteç kullanımı: giriş=1200 çıkış=300", "Belirteç kullanımı: giriş=1200 çıkış=300", "\u0000" },
        { "Pairing code expired: deviceId=agent-01", "Pairing code expired: deviceId=agent-01", "\u0000" },
        { "AuthenticationScheme: Bearer was challenged.", "AuthenticationScheme: Bearer was challenged.", "\u0000" },
        { "WWW-Authenticate: Bearer error=\"invalid_token\"", "WWW-Authenticate: Bearer error=\"invalid_token\"", "\u0000" },
        { "firstPass=3 secondPass=4 classPass=ok", "firstPass=3 secondPass=4 classPass=ok", "\u0000" },
        { "Provider OpenRouter: ApiKey not set (key Ai:ApiKeys:OpenRouter)", "Provider OpenRouter: ApiKey not set (key Ai:ApiKeys:OpenRouter)", "\u0000" },
        { "WHERE r.\"Token\" = @__token_0\nLIMIT 1", "WHERE r.\"Token\" = @__token_0\nLIMIT 1", "\u0000" },
        { "Request starting HTTP/1.1 GET http://localhost:5000/api/users/by-email/ali@example.com - -", "Request starting HTTP/1.1 GET http://localhost:5000/api/users/by-email/ali@example.com - -", "\u0000" },
        { "Ai:ApiKeys:Groq=\"gsk_live_9f8e7d6c5b4a3210fedcba\"", "Ai:ApiKeys:Groq=\"…cba\"", "9f8e7d6c5b4a" },
        { "Token value: abc123def456ghi", "Token value: ***", "abc123def456" },

        // Review r5 / hunt r6 (fix-7): '@' ile başlayan şifre, tip adları, kapalı ayraç, IPv6, bağlantı dizesinde '{'.
        { "Host=db;Username=app;Password=@dmin2024;Database=k", "Host=db;Username=app;Password=***;Database=k", "dmin2024" },
        { "Smtp password=@Sup3rS3cret, retrying", "Smtp password=***", "Sup3rS3cret" },
        { "Konsept.Api.Auth.PasswordPolicyException: Password must be at least 8 characters.", "Konsept.Api.Auth.PasswordPolicyException: Password must be at least 8 characters.", "\u0000" },
        { "Konsept.Api.Services.PasswordResetService: Reset link sent to user 42", "Konsept.Api.Services.PasswordResetService: Reset link sent to user 42", "\u0000" },
        { "spring.datasource.password=Hunter2Pass", "spring.datasource.password=***", "Hunter2Pass" },
        { "System.Exception: Credentials: [none]\n   at A.B()\n   at C.D()", "System.Exception: Credentials: ***\n   at A.B()\n   at C.D()", "[none]" },
        { "GET http://[::1]:5000/api/users/ali@example.com - -", "GET http://[::1]:5000/api/users/ali@example.com - -", "\u0000" },
        { "Host=db;Username=k;Password={abc}def123;Database=x", "Host=db;Username=k;Password=***;Database=x", "def123" },
        { "x-api-key:AbCdEfGhIjKlMnOpQrStUv", "x-api-key:…tUv", "AbCdEfGhIjKl" },

        // Review r6 / hunt r7 (fix-8): düz gizli ad noktalı yolda da gizli; tip/kod adları korunur; ODBC/JSON ayraç; EF 10; oturum.
        { "Smtp.Password: S3cretPass99", "Smtp.Password: ***", "S3cretPass99" },
        { "Input.Password: hunter2secret", "Input.Password: ***", "hunter2secret" },
        { "Driver={PostgreSQL};Server=db;Uid=app;Pwd={p@ss;w0rdTail};Database=k", "Driver={PostgreSQL};Server=db;Uid=app;Pwd=***;Database=k", "w0rdTail" },
        { "Credentials: {\"user\":\"app\",\"pwd\":\"P@ss;w0rd!23\"}", "Credentials: ***", "w0rd!23" },
        { "WHERE u.\"Token\" = @token\nLIMIT 1", "WHERE u.\"Token\" = @token\nLIMIT 1", "\u0000" },
        { "Errors: [PasswordRequiresDigit: Passwords must have at least one digit.], [DuplicateEmail: Email taken.]", "Errors: [PasswordRequiresDigit: Passwords must have at least one digit.], [DuplicateEmail: Email taken.]", "\u0000" },
        { "PasswordHashFormatException: unsupported version byte 0x02", "PasswordHashFormatException: unsupported version byte 0x02", "\u0000" },
        { "Session created for user 42: SessionId=Zk9xPq2Lm8Rt4Vw6", "Session created for user 42: SessionId=***", "Zk9xPq2Lm8Rt" },
        { "Oturum anahtarı: Zk9xPq2Lm8Rt", "Oturum anahtarı: ***", "Zk9xPq2Lm8Rt" },

        // Review r7 / hunt r8 (fix-9): istisnalar yalnızca kendi biçimlerinde; sid sıradan ad; yapılandırma yolu.
        { "Ai:ApiKeys:GeminiService=Zx9vQ2mL8pKr4aBc", "Ai:ApiKeys:GeminiService=…aBc", "Zx9vQ2mL8pKr" },
        { "PasswordResult=Hunter2Secret99", "PasswordResult=***", "Hunter2Secret99" },
        { "{\"Password\": @dmin2024, \"User\": \"ali\"}", "{\"Password\": ***, \"User\": \"ali\"}", "dmin2024" },
        { "pid=1234 sid=1234 tty=pts/0 cmd=dotnet Konsept.Api.dll", "pid=1234 sid=1234 tty=pts/0 cmd=dotnet Konsept.Api.dll", "\u0000" },
        { "sessionTokens=q8Zr3kLmN0pQ2sT4vW", "sessionTokens=***", "q8Zr3kLmN0pQ" },
        { "Failed to convert configuration value 'abc' at 'Identity:Password:RequiredLength' to type 'System.Int32'.", "Failed to convert configuration value 'abc' at 'Identity:Password:RequiredLength' to type 'System.Int32'.", "\u0000" },

        // Review r8 / hunt r9 (fix-10): yol istisnası daraltıldı; ayraç alıntısı; hata niteleyicisi; ';' içeren şifre; çerez adı.
        { "{\"detail\":\"password:Welcome\"}", "{\"detail\":\"password:***}", "Welcome" },
        { "'Password:Summer'", "'Password:***", "Summer" },
        { "{\"ConnectionStrings\":{\"Default\":\"Host=db;Password={Pa;ss1234}\"}}", "{\"ConnectionStrings\":{\"Default\":\"Host=db;Password=***\"}}", "ss1234" },
        { "Invalid password: user 42, remaining attempts 2.", "Invalid password: ***", "\u0000" },
        { "Hatalı şifre: kullanıcı 42, kalan hak 1", "Hatalı şifre: ***", "\u0000" },
        { "Invalid service key: agent 7 rejected from 192.168.1.40", "Invalid service key: ***", "\u0000" },
        { "Invalid password: hunter2.", "Invalid password: ***", "hunter2" },
        { "Password=Hunter;2024", "Password=***", "2024" },
        { "The required antiforgery cookie \".AspNetCore.Antiforgery.abc\" is not present.", "The required antiforgery cookie \".AspNetCore.Antiforgery.abc\" is not present.", "\u0000" },

        // Review r9 / hunt r10 (fix-11): niteleyici istisnası sızdırmaz; --cookie gizli; boşluklu bağlantı anahtarları; düz sözcük yayılmaz.
        { "Failed token=abc123def456 path=/api", "Failed token=***", "abc123def456" },
        { "Invalid password: hunter2 (attempt 3)", "Invalid password: ***", "hunter2" },
        { "Invalid password: \"hunter 2\" for bob", "Invalid password: \"***\" for bob", "hunter 2" },
        { "Wrong password 'hunter2' for bob", "Wrong password '***' for bob", "hunter2" },
        { "{\"msg\":\"Invalid password: hunter2\"}", "{\"msg\":\"Invalid password: ***}", "hunter2" },
        { "curl --cookie sid=abc123secret https://x", "curl --cookie *** https://x", "abc123secret" },
        { "Server=db;Database=app;Password=Secret1;User Id=sa;Encrypt=false", "Server=db;Database=app;Password=***;User Id=sa;Encrypt=false", "Secret1" },
        { "Validation failed: \n -- Password: 'Password' must not be empty.\n   at Konsept.Identity.PasswordPolicy.Validate()", "Validation failed: \n -- ***: '***' must not be empty.\n   at Konsept.Identity.PasswordPolicy.Validate()", "\u0000" },
        { "Scheme mismatch: got Bearer (expected Cookies)", "Scheme mismatch: got Bearer (expected Cookies)", "\u0000" },

        // Review r10 / hunt r11 (fix-12): niteleyici istisnası kaldırıldı; tek sözcük değer tam sözcük yayılır; düzyazıda ';'; sorgu değeri.
        { "Invalid password: correct horse battery staple", "Invalid password: ***", "horse battery" },
        { "Invalid password:  Sunshine2024", "Invalid password:  ***", "Sunshine2024" },
        { "Invalid cookie: theme=dark; .AspNetCore.Identity.Application=CfDJ8abcdef1234567890", "Invalid cookie: ***", "CfDJ8abcdef" },
        { "Invalid password: Sunshine. Please retry with Sunshine", "Invalid password: ***", "Sunshine" },
        { "{\"password\":\"Sunshine\",\"note\":\"user typed Sunshine again\"}", "{\"password\":\"***\",\"note\":\"user typed *** again\"}", "Sunshine" },
        { "User 42 signed in; SessionId=7f3a9c2e1b4d, ExpiresUtc=2026-11-28T10:00:00Z, Ip=192.168.1.10", "User 42 signed in; SessionId=***, ExpiresUtc=2026-11-28T10:00:00Z, Ip=192.168.1.10", "7f3a9c2e1b4d" },
        { "GET http://localhost:5000/hubs/agent?id=abc123&access_token=svc_9f8e7d6c5b4a3210 - 101 ms", "GET http://localhost:5000/hubs/agent?id=abc123&access_token=*** - 101 ms", "svc_9f8e7d6c" },
        { "q=user%253Dbob%2526password%253DSupersecretpw", "q=user%253Dbob%2526password%253D***", "Supersecretpw" },

        // Review r11 / hunt r12 (fix-13): kaçış sonrası tam sözcük; kodlu harf-yalnız değer; sorgu değerinde ",;"; köşeli değer kuyruğu.
        { "{\"password\":\"Sunshine\",\"history\":\"attempt1\\nSunshine again\"}", "{\"password\":\"***\",\"history\":\"attempt1\\n*** again\"}", "Sunshine" },
        { "{\"body\":\"{\\\"\\u015Fifre\\\":\\\"G\\u00FCne\\u015Fli\\\"}\"}", "{\"body\":\"{\\\"\\u015Fifre\\\":\\\"***\\\"}\"}", "ne\\u015Fli" },
        { "GET /cb?password=Pa,ss123 HTTP/1.1", "GET /cb?password=*** HTTP/1.1", "ss123" },
        { "RegisterCommand { Name = Ali, Password = {Hunter}2024 }", "RegisterCommand { Name = Ali, Password = *** }", "2024" },
        { "Login: email=ali@x.com, password=[Hunter]2024, ip=192.168.1.20", "Login: email=ali@x.com, password=***, ip=192.168.1.20", "2024" },

        // Review r12 / hunt r13 (fix-14): sorgu değeri tırnakta biter; ayraç + kuyruk; ",ad=" şifrede; tek tırnaklı çerez adı.
        { "{\"url\":\"/cb?token=abc123def\",\"user\":\"ali\",\"status\":401}", "{\"url\":\"/cb?token=***\",\"user\":\"ali\",\"status\":401}", "abc123def" },
        { "Password = {Blue,Sky:2024}!", "Password = ***", "Sky:2024" },
        { "Password: {redacted}. Login failed for user bob from 10.0.0.5", "Password: ***", "\u0000" },
        { "password=Pa55,w0rd=Q!zSecret user=42", "password=***", "Q!zSecret" },
        { "Login: Email = ali@example.com, Password = Ab,cd=Ef12345, Remember = False", "Login: Email = ali@example.com, Password = ***, Remember = False", "Ef12345" },
        { "The cookie '.AspNetCore.Cookies' has set 'SameSite=None' and must also set 'Secure'.", "The cookie '.AspNetCore.Cookies' has set 'SameSite=None' and must also set 'Secure'.", "\u0000" },

        // Review r13 / hunt r14 (fix-15): ayraç kuyruğu serbest metin kuralıyla; sorgu '"' yalnızca JSON kapanışı; çerez adı + değer; oturum/bilet/tuz adları.
        { "Password = {Hunter}2024 x9!", "Password = ***", "x9!" },
        { "Password = {Hunter}2024, x9!", "Password = ***", "x9!" },
        { "GET /cb?password=Ab\"cd123 HTTP/1.1", "GET /cb?password=*** HTTP/1.1", "cd123" },
        { "Appending cookie '.AspNetCore.Session=CfDJ8SessAbc12345' to response", "Appending cookie '***' to response", "CfDJ8Sess" },
        { "Session cookie issued: konsept_session=Zx9Yw8Vu7Ts6Rq5Po4; HttpOnly; Secure", "Session cookie issued: konsept_session=***", "Zx9Yw8Vu7" },
        { "Ticket for user 42 renewed: ticket=CfDJ8NrAkS1abc", "Ticket for user 42 renewed: ticket=***", "CfDJ8NrAk" },
        { "Salt=Zm9vYmFyYmF6cXV4MTIz, Iterations=600000", "Salt=***, Iterations=600000", "Zm9vYmFy" },

        // Hunt r15 (fix-16): sıkı JSON'da değişmez değer (null/true/false/sayı) sonraki alanları silmez; sayısal sır yine gizli.
        { "Pairing status: {\"status\":\"Pending\",\"agentToken\":null,\"expiresInSeconds\":240,\"machineName\":\"DESKTOP-01\"}", "Pairing status: {\"status\":\"Pending\",\"agentToken\":***,\"expiresInSeconds\":240,\"machineName\":\"DESKTOP-01\"}", "\u0000" },
        { "{\"token\":null,\"error\":\"invalid_credentials\",\"remainingAttempts\":2}", "{\"token\":***,\"error\":\"invalid_credentials\",\"remainingAttempts\":2}", "\u0000" },
        { "{\"pairingCode\":482913,\"agentId\":7}", "{\"pairingCode\":***,\"agentId\":7}", "482913" },
        { "\"password\": 2024 Summer2024", "\"password\": ***", "Summer2024" },
        { "\"password\":1234,Secret5678", "\"password\":***", "Secret5678" },
        { "Oturum açıldı: oturum=CfDJ8SessAbcdef1234567 kullanıcı=42", "Oturum açıldı: oturum=***", "CfDJ8Sess" },
        { "{\"oturum\":\"CfDJ8SessAbcdef1234567\",\"kullanici\":42}", "{\"oturum\":\"***\",\"kullanici\":42}", "CfDJ8Sess" },
        { "{\"password\":nullable123,\"x\":1}", "{\"password\":***}", "nullable123" },
    };

    /// <summary>64 KB üstü kesilen satırın sonundaki satır sonu korunur; sonraki kayıt aynı satıra yapışmaz (review r4, L-1).</summary>
    [Fact]
    public void Mask_OverMaxLength_KeepsTrailingNewline()
    {
        var masked = SecretMasker.Mask(new string('a', SecretMasker.MaxInputLength) + " tail\r\n");

        masked.ShouldEndWith(SecretMasker.TruncatedMarker + "\r\n");
    }

    /// <summary>Bağlantı dizesindeki yalnızca harfli şifre de bilinen sır olarak kaydedilir (düz sözcük filtresi kayda uygulanmaz).</summary>
    [Fact]
    public void RegisterConfigurationValue_LettersOnlyConnectionPassword_Registered()
    {
        SecretMasker.RegisterConfigurationValue("ConnectionStrings:Other", "Host=db;Username=app;Password=Lettersonlypw;Database=k");

        SecretMasker.Mask("retry with Lettersonlypw failed").ShouldBe("retry with *** failed");
    }

    /// <summary>Uygulamanın bildiği sır (kayıtlı) metnin herhangi bir yerinde, etiketsiz de olsa gizlenir (H3-H).</summary>
    [Fact]
    public void Mask_RegisteredKnownSecret_HiddenAnywhere()
    {
        SecretMasker.RegisterKnownSecret("KnownSecretXyz987");

        SecretMasker.Mask("connect failed using KnownSecretXyz987 (retry)").ShouldBe("connect failed using *** (retry)");
        SecretMasker.Mask("q=KnownSecretXyz987%21").ShouldNotContain("KnownSecretXyz987");
    }

    /// <summary>Yapılandırmadaki gizli girdiler (gizli ad, bağlantı dizesindeki şifre) bilinen sır olur; sıradan girdiler olmaz.</summary>
    [Fact]
    public void RegisterConfigurationValue_SecretEntriesOnly()
    {
        SecretMasker.RegisterConfigurationValue("ConnectionStrings:Konsept", "Host=db;Username=app;Password=CfgPwQwe123;Database=k");
        SecretMasker.RegisterConfigurationValue("Internal:ServiceKey", "SvcKeyAsd456789");
        SecretMasker.RegisterConfigurationValue("Logging:LogLevel:Default", "InformationLevelXyz");
        SecretMasker.RegisterConfigurationValue("Serilog:MinimumLevel:Override:Microsoft.AspNetCore.Authorization", "WarningLevelXyz");
        SecretMasker.RegisterConfigurationValue("Authentication:Cookie:Name", "Konsept.SessionXyz");
        SecretMasker.RegisterConfigurationValue("Ai:ApiKeys:Gemini", "GeminiCfgKey12345");

        SecretMasker.Mask("auth failed CfgPwQwe123 / SvcKeyAsd456789 / GeminiCfgKey12345").ShouldBe("auth failed *** / *** / ***");
        SecretMasker.Mask("level InformationLevelXyz WarningLevelXyz Konsept.SessionXyz")
            .ShouldBe("level InformationLevelXyz WarningLevelXyz Konsept.SessionXyz");
    }

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
    [InlineData("eyJ-")]
    [InlineData("\\u0022password\\u0022:\\u0022x ")]
    [InlineData("%22token%22%3A%22")]
    [InlineData("Pwd={a;")]
    [InlineData("Host=x;Password={a;")]
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

        // Ayraç yoksa kesim CutBackoff kadar geri çekilir: sınırdaki yarım token bile çıkmaz.
        masked.Length.ShouldBe(SecretMasker.MaxInputLength - SecretMasker.CutBackoff + SecretMasker.TruncatedMarker.Length);
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
