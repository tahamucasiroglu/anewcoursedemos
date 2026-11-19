using System.Globalization;
using Microsoft.Extensions.Configuration;
using OkulYonetim.WebAPI.Logging;
using Serilog;
using Serilog.Core;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Logging;

/// <summary>
/// Log yapılandırmasının testleri (spec 0001, AC-14): varsayılan seviye Information, ayarla Debug açılır,
/// klasör ayardan gelir, dosya günlük adlandırılır ve 30 günden eski dosyalar tutulmaz.
/// </summary>
public sealed class LoggingSetupTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "okul-log-test-" + Guid.NewGuid().ToString("N"));

    public LoggingSetupTests()
    {
        Directory.CreateDirectory(this.folder);
    }

    public void Dispose()
    {
        Directory.Delete(this.folder, recursive: true);
    }

    [Fact]
    public void DefaultLevel_IsInformation_DebugSuppressed()
    {
        this.WriteWith(new Dictionary<string, string?>(), logger =>
        {
            logger.Debug("debug-satiri");
            logger.Information("bilgi-satiri");
        });

        string content = this.ReadAll();
        content.ShouldContain("bilgi-satiri");
        content.ShouldNotContain("debug-satiri");
    }

    [Fact]
    public void ConfiguredDebug_WritesDebug()
    {
        this.WriteWith(new Dictionary<string, string?> { ["Serilog:MinimumLevel:Default"] = "Debug" }, logger => logger.Debug("debug-satiri"));

        this.ReadAll().ShouldContain("debug-satiri");
    }

    [Fact]
    public void FileSink_Retains30Days_DailyRolling()
    {
        string old = Path.Combine(this.folder, "okul-api-20200101.log");
        File.WriteAllText(old, "eski");
        string recent = Path.Combine(this.folder, "okul-api-" + DateTime.Now.AddDays(-5).ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
        File.WriteAllText(recent, "yeni");

        this.WriteWith(new Dictionary<string, string?>(), logger => logger.Information("bugun"));

        File.Exists(old).ShouldBeFalse("30 günden eski log dosyası silinmeli");
        File.Exists(recent).ShouldBeTrue("30 günden yeni log dosyası kalmalı");
        string today = Path.Combine(this.folder, "okul-api-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
        File.Exists(today).ShouldBeTrue("bugünün dosyası okul-api-YYYYMMDD.log adıyla açılmalı");
    }

    private void WriteWith(Dictionary<string, string?> settings, Action<Logger> write)
    {
        settings["FileLogging:Folder"] = this.folder;
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        using Logger logger = LoggingSetup.Configure(new LoggerConfiguration(), configuration).CreateLogger();
        write(logger);
    }

    private string ReadAll() =>
        string.Concat(Directory.GetFiles(this.folder, "okul-api-*.log").Select(File.ReadAllText));
}
