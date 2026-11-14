using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Agents.Infrastructure.Logging.Concrete;
using TahaKonsept.Modules.Agents.Tests.Support;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Diagnostics;
using Xunit;

namespace TahaKonsept.Modules.Agents.Tests.Logging;

/// <summary>
/// Ajan günlüğü → Diagnostics işlem günlüğü köprüsü: kayıt ajanın kullanıcısı adına <c>Agent</c> kaynağıyla yazılır, Debug atlanır.
/// </summary>
public sealed class ActivityAgentLogSinkTests
{
    private readonly RecordingActivityLogger _activity = new();
    private readonly ActivityAgentLogSink _sink;

    /// <summary>Kurucu.</summary>
    public ActivityAgentLogSinkTests()
    {
        _sink = new ActivityAgentLogSink(new LoggingAgentLogSink(NullLogger<LoggingAgentLogSink>.Instance), _activity);
    }

    /// <summary>Bilgi/uyarı/hata kayıtları kullanıcının işlem günlüğüne düşer.</summary>
    [Fact]
    public async Task Writes_entry_for_agent_user()
    {
        var userId = Guid.NewGuid();
        var context = new AgentCallContext("c1", userId, Guid.NewGuid());

        await _sink.WriteAsync(
            context,
            new AgentLogEntry { Level = LogLevelKind.Warning, Category = "Speech", Message = "Model indiriliyor", JobId = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        var entry = _activity.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevelKind.Warning);
        entry.Source.ShouldBe(ActivitySources.Agent);
        entry.Message.ShouldBe("Model indiriliyor");
        entry.UserId.ShouldBe(userId);
        entry.Details.ShouldNotBeNull();
    }

    /// <summary>Debug kayıtları yalnızca teknik günlüğe gider.</summary>
    [Fact]
    public async Task Skips_debug_entries()
    {
        await _sink.WriteAsync(
            new AgentCallContext("c1", Guid.NewGuid(), null),
            new AgentLogEntry { Level = LogLevelKind.Debug, Message = "ayrıntı" },
            TestContext.Current.CancellationToken);

        _activity.Entries.ShouldBeEmpty();
    }
}
