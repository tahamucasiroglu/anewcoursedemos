using Konsept.Modules.AgentGateway.Application.Base;
using Konsept.Modules.AgentGateway.Application.Concrete;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Infrastructure.Concrete;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Konsept.Modules.AgentGateway.Tests;

/// <summary>Ajan log özeti kabul kuralları (spec 0008 AC-7; review L-2, Risk 6).</summary>
public sealed class AgentLogServiceTests
{
    private readonly InMemoryAgentConnectionRegistry _registry = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));
    private readonly ITechnicalLogWriter _writer = Substitute.For<ITechnicalLogWriter>();
    private readonly List<TechnicalLogEntry> _written = [];

    /// <summary>Initializes a new instance of the <see cref="AgentLogServiceTests"/> class.</summary>
    public AgentLogServiceTests()
    {
        _registry.Set(new AgentSession("conn-1", "TAHA-PC", "0.2.0", _time.GetUtcNow()));
        _writer.WriteAsync(Arg.Do<IReadOnlyList<TechnicalLogEntry>>(e => _written.AddRange(e)), Arg.Any<CancellationToken>()).Returns(1);
    }

    /// <summary>Boş (null) mesaj hata vermez; mesaj önce maskelenir sonra kırpılır, key parçası sızmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Report_NullOrLongMessage_SafeAndMaskedBeforeClip()
    {
        var service = new AgentLogService(_registry, new AgentLogQuota(), _time, _writer);
        var longMessage = new string('x', 3990) + " sk-proj-ABCDEFGHIJKLMNOP-zz9";

        (await service.ReportAsync("conn-1", new AgentLogSummary(4, _time.GetUtcNow(), null!, "0.2.0"), TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await service.ReportAsync("conn-1", new AgentLogSummary(4, _time.GetUtcNow(), longMessage, "0.2.0"), TestContext.Current.CancellationToken)).ShouldBeTrue();

        _written[0].Message.ShouldBe(string.Empty);
        _written[1].Message.Length.ShouldBeLessThanOrEqualTo(4000);
        _written[1].Message.ShouldNotContain("ABCDEFG");

        // Önce kırpılsaydı 4000. karakterde "sk-proj-A" kalır (gövde 6 < 8, desen yakalamaz) ve sızardı.
        _written[1].Message.ShouldNotContain("sk-proj");
        _written[1].Message.ShouldEndWith("sk-…zz9");
    }

    /// <summary>
    /// 8000 karakterden uzun mesajda kaba kesim noktasına denk gelen key'in yarım parçası son mesajda kalmaz; maskeleme
    /// metni kısaltsa bile (review round 3, L-1).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Report_KeyCutAtCoarseLimit_PartialKeyDropped()
    {
        var service = new AgentLogService(_registry, new AgentLogQuota(), _time, _writer);
        var keys = string.Join(' ', Enumerable.Repeat("sk-proj-ABCDEFGHIJKLMNOPQRSTUV", 300));
        var prefix = keys[..7980].PadRight(7993, 'x') + " ";
        var message = prefix + "sk-REALSECRETKEY1234567890 son";

        (await service.ReportAsync("conn-1", new AgentLogSummary(4, _time.GetUtcNow(), message, "0.2.0"), TestContext.Current.CancellationToken)).ShouldBeTrue();

        AgentLogServiceBase.CoarseCut(message).Length.ShouldBeLessThanOrEqualTo(AgentLogServiceBase.MaxMessageLength * 2);
        _written.Single().Message.ShouldNotContain("sk-REA");
        _written.Single().Message.ShouldNotContain("REALSECRET");
    }

    /// <summary>Dakikada 60'tan fazla özet sessizce düşer; yeni dakikada yeniden kabul edilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Report_OverQuota_DroppedUntilNextMinute()
    {
        var service = new AgentLogService(_registry, new AgentLogQuota(), _time, _writer);
        var accepted = 0;
        for (var i = 0; i < 61; i++)
        {
            if (await service.ReportAsync("conn-1", new AgentLogSummary(4, _time.GetUtcNow(), $"h{i}", "0.2.0"), TestContext.Current.CancellationToken))
            {
                accepted++;
            }
        }

        _time.Advance(TimeSpan.FromMinutes(1));
        var afterMinute = await service.ReportAsync("conn-1", new AgentLogSummary(4, _time.GetUtcNow(), "yeni", "0.2.0"), TestContext.Current.CancellationToken);

        accepted.ShouldBe(AgentLogQuota.PerMinute);
        afterMinute.ShouldBeTrue();
    }
}
