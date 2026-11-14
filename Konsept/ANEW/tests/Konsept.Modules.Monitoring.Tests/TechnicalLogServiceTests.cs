using Konsept.Modules.Monitoring.Application;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Concrete;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Konsept.Modules.Monitoring.Tests;

/// <summary>
/// Olay → teknik log eşlemesi (spec 0008 AC-3), teknik log servisi (AC-6 kuralları) ve kayıt sınırında maskeleme
/// (AC-8: olay kaydı ve teknik log kaydı).
/// </summary>
public sealed class TechnicalLogServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 14, 9, 30, 0, TimeSpan.Zero));

    /// <summary>Olay kaydına yazılan olay teknik logda da bulunur; seviyesi eşlemeye uyar.</summary>
    /// <param name="level">Olay seviyesi.</param>
    /// <param name="expected">Beklenen teknik log seviyesi.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData(SystemEventLevel.Info, LogLevel.Information)]
    [InlineData(SystemEventLevel.Warning, LogLevel.Warning)]
    [InlineData(SystemEventLevel.Error, LogLevel.Error)]
    [InlineData(SystemEventLevel.Critical, LogLevel.Critical)]
    public async Task SystemEvent_Written_AlsoInTechnicalLogWithMappedLevel(SystemEventLevel level, LogLevel expected)
    {
        var logger = new ListLogger<SystemEventService>();
        var service = new SystemEventService(Substitute.For<ISystemEventRepository>(), _time, logger);

        await service.WriteAsync(new SystemEventEntry("agent.connected", level, "Ajan bağlandı: TAHA-PC (v0.2.0)"), TestContext.Current.CancellationToken);

        var entry = logger.Entries.Single();
        entry.Level.ShouldBe(expected);
        entry.Message.ShouldContain("agent.connected");
        entry.Message.ShouldContain("Ajan bağlandı: TAHA-PC (v0.2.0)");
    }

    /// <summary>Veritabanı yazılamasa bile olay önce teknik loga yazılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SystemEvent_PersistFails_StillInTechnicalLog()
    {
        var logger = new ListLogger<SystemEventService>();
        var repository = Substitute.For<ISystemEventRepository>();
        repository.AddAsync(Arg.Any<SystemEvent>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("db yok")));
        var service = new SystemEventService(repository, _time, logger);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            service.WriteAsync(new SystemEventEntry("agent.connected", SystemEventLevel.Info, "Ajan bağlandı"), TestContext.Current.CancellationToken));

        logger.Entries.Count.ShouldBe(1);
    }

    /// <summary>Olay metnindeki gizli bilgi olay kaydına ve teknik loga maskeli yazılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SystemEvent_SecretInMessage_MaskedInEventAndLog()
    {
        var logger = new ListLogger<SystemEventService>();
        var repository = Substitute.For<ISystemEventRepository>();
        var service = new SystemEventService(repository, _time, logger);

        var dto = await service.WriteAsync(
            new SystemEventEntry("test", SystemEventLevel.Warning, "key sk-ant-api03-EVENTSECRET123-q1w ve Password=evt-pw"),
            TestContext.Current.CancellationToken);

        dto.Message.ShouldBe("key sk-ant-…q1w ve Password=***");
        await repository.Received(1).AddAsync(Arg.Is<SystemEvent>(e => e.Message == dto.Message), Arg.Any<CancellationToken>());
        logger.Entries.Single().Message.ShouldNotContain("EVENTSECRET123");
    }

    /// <summary>Information kayıtları veritabanına yazılmaz; Warning, Error, Critical yazılır ve maskelenir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TechnicalLogService_Write_DropsInformationMasksRest()
    {
        var repository = Substitute.For<ITechnicalLogRepository>();
        IReadOnlyList<TechnicalLog>? saved = null;
        await repository.AddRangeAsync(Arg.Do<IReadOnlyList<TechnicalLog>>(l => saved = l), Arg.Any<CancellationToken>());
        var service = new TechnicalLogService(repository, _time);
        var now = _time.GetUtcNow();

        var count = await service.WriteAsync(
            [
                new TechnicalLogEntry(now, LogEntryLevel.Information, LogEntrySource.Api, "bilgi"),
                new TechnicalLogEntry(now, LogEntryLevel.Warning, LogEntrySource.Api, "uyarı sk-proj-SERVICEKEY1234-z9y"),
                new TechnicalLogEntry(now, LogEntryLevel.Error, LogEntrySource.Api, "hata", "System.Exception: Password=exc-1"),
                new TechnicalLogEntry(now, LogEntryLevel.Critical, LogEntrySource.Api, "kritik"),
            ],
            TestContext.Current.CancellationToken);

        count.ShouldBe(3);
        saved!.Select(l => l.Level).ShouldBe([TechnicalLogLevel.Warning, TechnicalLogLevel.Error, TechnicalLogLevel.Critical]);
        saved![0].Message.ShouldBe("uyarı sk-…z9y");
        saved[1].Exception.ShouldBe("System.Exception: Password=***");
    }

    /// <summary>Web'in ilettiği kayıtlar kaynak ne gönderilirse gönderilsin Web olarak yazılır; 200'ü aşan istek reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TechnicalLogService_AcceptWebLogs_ForcesWebSourceAndLimit()
    {
        var repository = Substitute.For<ITechnicalLogRepository>();
        IReadOnlyList<TechnicalLog>? saved = null;
        await repository.AddRangeAsync(Arg.Do<IReadOnlyList<TechnicalLog>>(l => saved = l), Arg.Any<CancellationToken>());
        var service = new TechnicalLogService(repository, _time);
        var entry = new TechnicalLogEntry(_time.GetUtcNow(), LogEntryLevel.Error, LogEntrySource.Agent, "hata", AgentVersion: "9.9.9");

        var ok = await service.AcceptWebLogsAsync([entry], TestContext.Current.CancellationToken);
        var tooMany = await service.AcceptWebLogsAsync([.. Enumerable.Repeat(entry, MonitoringErrors.MaxLogsPerRequest + 1)], TestContext.Current.CancellationToken);

        ok.Value.ShouldBe(1);
        saved!.Single().Source.ShouldBe(LogSource.Web);
        saved!.Single().AgentVersion.ShouldBeNull();
        tooMany.Error.ShouldBe(MonitoringErrors.TooManyLogs);
    }

    /// <summary>Saklama sınırı şimdi − 30 gündür (30 günlük korunur, daha eskisi silinir).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TechnicalLogService_Purge_CutoffIsThirtyDays()
    {
        var repository = Substitute.For<ITechnicalLogRepository>();
        var service = new TechnicalLogService(repository, _time);

        await service.PurgeExpiredAsync(TestContext.Current.CancellationToken);

        await repository.Received(1).DeleteOlderThanAsync(_time.GetUtcNow().AddDays(-30), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sunucu saatinden 5 dakikadan fazla sapan gönderilmiş zaman sunucu saatine çekilir (gelecek tarihli kayıt saklamadan
    /// kaçamaz, geçmiş tarihli kayıt hemen silinmez); sınır içindeki zaman korunur (review L-1).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TechnicalLogService_Write_ClampsClientClock()
    {
        var repository = Substitute.For<ITechnicalLogRepository>();
        IReadOnlyList<TechnicalLog>? saved = null;
        await repository.AddRangeAsync(Arg.Do<IReadOnlyList<TechnicalLog>>(l => saved = l), Arg.Any<CancellationToken>());
        var service = new TechnicalLogService(repository, _time);
        var now = _time.GetUtcNow();

        await service.WriteAsync(
            [
                new TechnicalLogEntry(new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeSpan.Zero), LogEntryLevel.Error, LogEntrySource.Agent, "gelecek"),
                new TechnicalLogEntry(now.AddDays(-40), LogEntryLevel.Error, LogEntrySource.Agent, "geçmiş"),
                new TechnicalLogEntry(now.AddMinutes(-4), LogEntryLevel.Error, LogEntrySource.Agent, "yakın"),
            ],
            TestContext.Current.CancellationToken);

        saved![0].OccurredAt.ShouldBe(now);
        saved[1].OccurredAt.ShouldBe(now);
        saved[2].OccurredAt.ShouldBe(now.AddMinutes(-4));
    }

    /// <summary>Listede null eleman ya da tanımsız seviye/kaynak varsa o kayıt atlanır; istek hata vermez (review L-2).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TechnicalLogService_Write_SkipsNullAndUndefinedValues()
    {
        var repository = Substitute.For<ITechnicalLogRepository>();
        IReadOnlyList<TechnicalLog>? saved = null;
        await repository.AddRangeAsync(Arg.Do<IReadOnlyList<TechnicalLog>>(l => saved = l), Arg.Any<CancellationToken>());
        var service = new TechnicalLogService(repository, _time);
        var now = _time.GetUtcNow();

        var count = await service.WriteAsync(
            [
                null!,
                new TechnicalLogEntry(now, (LogEntryLevel)99, LogEntrySource.Web, "seviye 99"),
                new TechnicalLogEntry(now, LogEntryLevel.Error, (LogEntrySource)7, "kaynak 7"),
                new TechnicalLogEntry(now, LogEntryLevel.Error, LogEntrySource.Web, "geçerli"),
            ],
            TestContext.Current.CancellationToken);

        count.ShouldBe(1);
        saved!.Single().Message.ShouldBe("geçerli");
    }
}
