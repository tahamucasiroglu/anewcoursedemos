using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Infrastructure.Concrete;
using Shouldly;

namespace Konsept.Modules.AgentGateway.Tests;

/// <summary>
/// Açık bağlantı izleyicisi (spec 0012 AC-7, AC-8; review r1 M-1): kimliği doğrulanmış ama henüz izlenmeyen bağlantının cihazı
/// arada iptal edilirse bağlantı sonradan alınmaz; iptal edilen cihazın açık bağlantıları kesilmek üzere verilir.
/// </summary>
public sealed class AgentConnectionTrackerTests
{
    /// <summary>İptalden sonra gelen aynı cihazın bağlantısı reddedilir; başka cihaz etkilenmez.</summary>
    [Fact]
    public void Track_AfterRevocation_RejectedOtherDeviceAccepted()
    {
        var tracker = new AgentConnectionTracker();
        var user = Guid.NewGuid();
        var revoked = Guid.NewGuid();
        var open = 0;
        tracker.Track("acik", new AgentOwner(user, revoked), () => open++).ShouldBeTrue();

        var taken = tracker.Take(user, [revoked]);
        var late = tracker.Track("gec", new AgentOwner(user, revoked), () => { });
        var other = tracker.Track("yeni", new AgentOwner(user, Guid.NewGuid()), () => { });

        taken.Select(t => t.Key).ShouldBe(["acik"]);
        taken[0].Value();
        open.ShouldBe(1);
        late.ShouldBeFalse();
        tracker.Contains("gec").ShouldBeFalse();
        tracker.Contains("acik").ShouldBeFalse();
        other.ShouldBeTrue();
        tracker.Contains("yeni").ShouldBeTrue();
    }
}
