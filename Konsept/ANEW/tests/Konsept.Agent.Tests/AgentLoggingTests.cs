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
            lines.ShouldAllBe(l => l.Contains(" Agent ") && l.Contains('Z'));
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
            using (var logging = AgentLogging.Create(dir.FullName, TimeProvider.System))
            {
                logging.Directory.ShouldBe(Path.Combine(dir.FullName, "logs"));
                logging.Logger.Warning("uyarı");
            }

            Directory.GetFiles(dir.FullName, "*", SearchOption.AllDirectories)
                .ShouldAllBe(f => Path.GetDirectoryName(f) == Path.Combine(dir.FullName, "logs") && Path.GetFileName(f).StartsWith("agent-", StringComparison.Ordinal));
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
}
