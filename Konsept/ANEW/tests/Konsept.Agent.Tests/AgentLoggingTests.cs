using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Infrastructure.Logging;
using NSubstitute;
using Serilog;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Ajan teknik logu (spec 0008 AC-1, AC-5, AC-7, AC-8 özet kısmı): dosya yalnızca ajan klasörü altındaki
/// <c>logs</c>'ta, kaynak "Agent"; API'ye yalnızca Error/Critical özetleri, maskeli.
/// </summary>
public sealed class AgentLoggingTests
{
    /// <summary>Gets AC-8 tam alan tablosundaki alan adları.</summary>
    public static TheoryData<string> SecretFields => [.. Konsept.Testing.SecretFieldSamples.All.Select(s => s.Field)];

    /// <summary>Ajan dört seviyede yazar; her satırda UTC zaman, seviye ve kaynak "Agent" bulunur.</summary>
    [Fact]
    public void AgentLog_WritesToFileWithSource()
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-agentlog-");
        try
        {
            using (var logging = AgentLogging.Create(dir.FullName, TimeProvider.System))
            {
                logging.Logger.Information("bilgi");
                logging.Logger.Warning("uyarı");
                logging.Logger.Error("hata");
                logging.Logger.Fatal("kritik");
            }

            var lines = File.ReadAllLines(Directory.GetFiles(Path.Combine(dir.FullName, "logs")).Single());
            lines.Length.ShouldBe(4);
            lines.ShouldAllBe(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d\.\d{3}Z \[(INF|WRN|ERR|FTL)\] Agent "));
            lines.Select(l => l.Substring(25, 5)).ShouldBe(["[INF]", "[WRN]", "[ERR]", "[FTL]"]);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>Ajan logları yalnızca ajan klasörü altındaki <c>logs</c> klasörüne <c>agent-</c> önekiyle yazılır.</summary>
    [Fact]
    public void AgentLog_Folder_IsBaseDirectoryLogs()
    {
        var dir = Directory.CreateTempSubdirectory("anewconcept-agentlog-");
        try
        {
            var defaultLogs = Path.Combine(AppContext.BaseDirectory, "logs");
            var defaultBefore = Directory.Exists(defaultLogs) ? Directory.GetFiles(defaultLogs).Order(StringComparer.Ordinal).ToArray() : [];
            using (var logging = AgentLogging.Create(dir.FullName, TimeProvider.System))
            {
                logging.Directory.ShouldBe(Path.Combine(dir.FullName, "logs"));
                logging.Logger.Warning("uyarı");
            }

            Directory.GetFiles(dir.FullName, "*", SearchOption.AllDirectories)
                .ShouldAllBe(f => Path.GetDirectoryName(f) == Path.Combine(dir.FullName, "logs") && Path.GetFileName(f).StartsWith("agent-", StringComparison.Ordinal));
            Directory.GetFiles(Path.Combine(dir.FullName, "logs")).Length.ShouldBe(1);
            (Directory.Exists(defaultLogs) ? Directory.GetFiles(defaultLogs).Order(StringComparer.Ordinal).ToArray() : []).ShouldBe(defaultBefore);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Özet sink'i yalnızca Error ve Critical'i bağlantıya iletir; Information ve Warning iletilmez; özet maskelidir
    /// ve yığın izi içermez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task HubLogSummarySink_OnlyErrorCritical_NotFileOrLowerLevels()
    {
        var connection = Substitute.For<IServerConnection>();
        var sent = new List<LogSummary>();
        connection.TryReportLogAsync(Arg.Do<LogSummary>(sent.Add), Arg.Any<CancellationToken>()).Returns(true);
        var sink = new HubLogSummarySink();
        sink.Attach(connection);
        var events = new List<Serilog.Events.LogEvent>();
        using (var logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(new DelegateSink(events.Add)).CreateLogger())
        {
            logger.Information("bilgi");
            logger.Warning("uyarı");
            logger.Error(new InvalidOperationException("Password=sum-pw-1"), "hata {ApiKey}", "sk-proj-SUMMARYKEY123-s2m");
            logger.Fatal("kritik");
        }

        await sink.EmitBatchAsync(events);

        sent.Count.ShouldBe(2);
        sent[0].Critical.ShouldBeFalse();
        sent[1].Critical.ShouldBeTrue();
        sent[0].Message.ShouldNotContain("SUMMARYKEY123");
        sent[0].Message.ShouldNotContain("sum-pw-1");
        sent[0].Message.ShouldContain("InvalidOperationException");
        sent[0].Message.ShouldNotContain(" at ");
    }

    /// <summary>Bağlantı bağlanmamışsa (ya da bağlı değilse) özet gönderilmez ve hata oluşmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task HubLogSummarySink_NoConnection_DropsSilently()
    {
        var sink = new HubLogSummarySink();
        var events = new List<Serilog.Events.LogEvent>();
        using (var logger = new LoggerConfiguration().WriteTo.Sink(new DelegateSink(events.Add)).CreateLogger())
        {
            logger.Error("hata");
        }

        await Should.NotThrowAsync(() => sink.EmitBatchAsync(events));
    }

    /// <summary>
    /// AC-8 ajan özeti yolu, tam alan tablosu (review round 1, M-3): gerçek ajan log kurulumunda adlandırılmış gizli alan
    /// içeren Error, bağlantıya maskeli özet olarak gider.
    /// </summary>
    /// <param name="field">Gizli alan adı.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [MemberData(nameof(SecretFields))]
    public async Task Masking_EverySecretField_NeverPlainInSummary(string field)
    {
        var sample = Konsept.Testing.SecretFieldSamples.Get(field);
        var connection = Substitute.For<IServerConnection>();
        var sent = new System.Collections.Concurrent.ConcurrentQueue<LogSummary>();
        connection.TryReportLogAsync(Arg.Do<LogSummary>(sent.Enqueue), Arg.Any<CancellationToken>()).Returns(true);
        var dir = Directory.CreateTempSubdirectory("anewconcept-agentlog-");
        try
        {
            using (var logging = AgentLogging.Create(dir.FullName, TimeProvider.System))
            {
                logging.SummarySink.Attach(connection);
#pragma warning disable CA2254 // Test: alan adı tablodan geliyor.
                logging.Logger.Error($"hata {{{field}}}", sample.Value);
#pragma warning restore CA2254
                var until = DateTime.UtcNow.AddSeconds(10);
                while (sent.IsEmpty && DateTime.UtcNow < until)
                {
                    await Task.Delay(100, TestContext.Current.CancellationToken);
                }
            }

            var summary = sent.ShouldHaveSingleItem();
            summary.Message.ShouldStartWith("hata ");
            summary.Message.ShouldNotContain(sample.Secret);
            File.ReadAllText(Directory.GetFiles(Path.Combine(dir.FullName, "logs")).Single()).ShouldNotContain(sample.Secret);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
