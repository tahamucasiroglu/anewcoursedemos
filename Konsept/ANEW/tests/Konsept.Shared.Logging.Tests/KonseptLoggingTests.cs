using Konsept.Shared.Logging.Concrete;
using Microsoft.Extensions.Time.Testing;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Shouldly;

namespace Konsept.Shared.Logging.Tests;

/// <summary>
/// Ortak teknik log kurulumu (spec 0008 AC-1, AC-4, AC-5, AC-8): seviye, UTC zaman, kaynak, korelasyon,
/// maskeleme ve dosya saklama.
/// </summary>
public sealed class KonseptLoggingTests
{
    /// <summary>Gets her kaynak uygulama.</summary>
    public static TheoryData<string> Sources => [LogSources.Web, LogSources.Api, LogSources.Agent];

    /// <summary>Her uygulamanın kaydında zaman (UTC), seviye, kaynak ve varsa korelasyon numarası bulunur.</summary>
    /// <param name="source">Kaynak uygulama.</param>
    [Theory]
    [MemberData(nameof(Sources))]
    public void Logging_EachApp_WritesLevelSourceUtcAndCorrelation(string source)
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-log-");
        try
        {
            var capture = new CapturingSink();
            using (var logger = new LoggerConfiguration().MinimumLevel.Information()
                       .WithKonseptEnrichment(source).WriteTo.Sink(capture).WriteToKonseptFile(dir.FullName, "app").CreateLogger())
            {
                using (LogContext.PushProperty(KonseptLogging.CorrelationProperty, "0HN-corr-42"))
                {
                    logger.Information("bilgi");
                    logger.Warning("uyarı");
                    logger.Error("hata");
                    logger.Fatal("kritik");
                }
            }

            capture.Events.Select(e => e.Level).ShouldBe([LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal]);
            foreach (var e in capture.Events)
            {
                e.Properties[KonseptLogging.ApplicationProperty].ToString().ShouldBe($"\"{source}\"");
                e.Properties[KonseptLogging.CorrelationProperty].ToString().ShouldBe("\"0HN-corr-42\"");
                ((DateTime)((ScalarValue)e.Properties[UtcTimestampEnricher.PropertyName]).Value!).Kind.ShouldBe(DateTimeKind.Utc);
            }

            var lines = File.ReadAllLines(Directory.GetFiles(dir.FullName).Single());
            lines.Length.ShouldBe(4);
            lines[0].ShouldMatch($@"^\d{{4}}-\d\d-\d\dT\d\d:\d\d:\d\d\.\d{{3}}Z \[INF\] {source} 0HN-corr-42 .*bilgi$");
            lines[3].ShouldContain("[FTL]");
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Gizli alanlar adlandırılmış özellik, iç içe nesne, mesaj şablonu ya da exception metni olarak girse bile dosyada
    /// düz metin görünmez; şifre maskeli bile görünmez; API key <c>sk-…a3f</c> biçimindedir (AC-8, dosya kısmı).
    /// </summary>
    [Fact]
    public void Masking_NamedSecretFields_NeverPlainInFile()
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-log-");
        try
        {
            var capture = new CapturingSink();
            using (var logger = new LoggerConfiguration().WithKonseptEnrichment(LogSources.Api)
                       .WriteTo.Sink(capture).WriteToKonseptFile(dir.FullName, "api").CreateLogger())
            {
                logger.Information(
                    "Giriş {Password} {ApiKey} {AccessToken} {RefreshToken} {PairingCode} {PairingSecret} {StreamToken} {ConnectionString}",
                    "hunter2-pw", "sk-proj-AbCdEf123456789a3f", "acc-opaque-1", "ref-opaque-2", "PAIR-778899", "pair-secret-3", "stream-tok-4",
                    "Host=pi;Password=db-pass-5;Username=u");
                logger.Information("Kayıt {@Request}", new { Email = "a@b.c", Password = "nested-pw-6", Inner = new { ApiKey = "sk-ant-api03-NESTEDNESTED-n7x" } });
                logger.Information("Şablona gömülü sk-proj-TEMPLATEKEY12345-t8q");
                logger.Error(new InvalidOperationException("bağlantı: Password=exc-pass-9; key sk-or-v1-EXCEPTIONKEY-e1z"), "hata");
            }

            var text = File.ReadAllText(Directory.GetFiles(dir.FullName).Single());
            string[] plain = ["hunter2-pw", "AbCdEf123456789", "acc-opaque-1", "ref-opaque-2", "PAIR-778899", "pair-secret-3", "stream-tok-4",
                "db-pass-5", "nested-pw-6", "NESTEDNESTED", "TEMPLATEKEY12345", "exc-pass-9", "EXCEPTIONKEY"];
            foreach (var secret in plain)
            {
                text.ShouldNotContain(secret);
            }

            text.ShouldContain("sk-…a3f");
            text.ShouldContain("sk-ant-…n7x");
            text.ShouldContain("Password=***");
            var first = capture.Events.First();
            first.Properties["Password"].ToString().ShouldBe("\"***\"");
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>Log dosyası yalnızca verilen klasöre yazılır, başka yerde dosya oluşmaz (AC-5).</summary>
    [Fact]
    public void LogDirectory_OnlyConfiguredFolder_NoFilesElsewhere()
    {
        var root = Directory.CreateTempSubdirectory("anewconcept-log-root-");
        try
        {
            var logs = Path.Combine(root.FullName, "logs");
            using (var logger = new LoggerConfiguration().WithKonseptEnrichment(LogSources.Web).WriteToKonseptFile(logs, "web").CreateLogger())
            {
                logger.Warning("uyarı");
            }

            Directory.GetFiles(root.FullName, "*", SearchOption.AllDirectories)
                .ShouldAllBe(f => Path.GetDirectoryName(f) == logs && Path.GetFileName(f).StartsWith("web-", StringComparison.Ordinal));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    /// <summary>7 günlük log dosyası korunur, 8 günlük silinir; diğer adlar ve uzantılar dokunulmaz (AC-4).</summary>
    /// <param name="prefix">Uygulama öneki.</param>
    [Theory]
    [InlineData("api")]
    [InlineData("web")]
    [InlineData("agent")]
    public void LogFileJanitor_Keeps7DayOld_Deletes8DayOld(string prefix)
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-janitor-");
        try
        {
            var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 14, 23, 30, 0, TimeSpan.Zero));
            string[] names = [$"{prefix}-20261114.log", $"{prefix}-20261107.log", $"{prefix}-20261106.log", $"{prefix}-20261106_001.log",
                $"{prefix}-20250101.log", "notes.txt", "other.log"];
            foreach (var name in names)
            {
                File.WriteAllText(Path.Combine(dir.FullName, name), "x");
            }

            var deleted = new LogFileJanitor(time).Clean(dir.FullName);

            deleted.ShouldBe(3);
            Directory.GetFiles(dir.FullName).Select(Path.GetFileName).Order(StringComparer.Ordinal)
                .ShouldBe(new[] { $"{prefix}-20261107.log", $"{prefix}-20261114.log", "notes.txt", "other.log" }.Order(StringComparer.Ordinal));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>Klasör yoksa temizlik hata vermez.</summary>
    [Fact]
    public void LogFileJanitor_MissingFolder_NoOp() =>
        new LogFileJanitor(TimeProvider.System).Clean(Path.Combine(Path.GetTempPath(), "anewconcept-yok-" + Guid.NewGuid())).ShouldBe(0);
}
