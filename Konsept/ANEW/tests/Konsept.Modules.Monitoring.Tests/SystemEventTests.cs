using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Shouldly;

namespace Konsept.Modules.Monitoring.Tests;

/// <summary>
/// Olay entity'sinin kuralları (spec 0001 Req 9).
/// </summary>
public sealed class SystemEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 12, 0, 0, TimeSpan.FromHours(3));

    /// <summary>Olay alanları ayrı ayrı tutulur, zaman UTC'ye çevrilir, kullanıcı boş olabilir.</summary>
    [Fact]
    public void Create_ValidInput_StoresFieldsSeparatelyInUtc()
    {
        var e = SystemEvent.Create("agent.connected", EventLevel.Info, "Ajan bağlandı", Now, userId: null);

        e.Type.ShouldBe("agent.connected");
        e.Level.ShouldBe(EventLevel.Info);
        e.Message.ShouldBe("Ajan bağlandı");
        e.OccurredAt.Offset.ShouldBe(TimeSpan.Zero);
        e.OccurredAt.ShouldBe(Now);
        e.UserId.ShouldBeNull();
        e.Id.Version.ShouldBe(7);
    }

    /// <summary>Çok uzun metin sınırda kırpılır.</summary>
    [Fact]
    public void Create_MessageTooLong_IsTruncated()
    {
        var e = SystemEvent.Create("t", EventLevel.Warning, new string('ı', SystemEvent.MessageMaxLength + 5), Now, null);

        e.Message.Length.ShouldBe(SystemEvent.MessageMaxLength);
    }

    /// <summary>Boş tür ya da metin kabul edilmez.</summary>
    /// <param name="type">Tür.</param>
    /// <param name="message">Metin.</param>
    [Theory]
    [InlineData("", "metin")]
    [InlineData("tür", " ")]
    public void Create_EmptyTypeOrMessage_Throws(string type, string message) =>
        Should.Throw<ArgumentException>(() => SystemEvent.Create(type, EventLevel.Info, message, Now, null));

    /// <summary>Tür alanı sınırı aşılırsa reddedilir.</summary>
    [Fact]
    public void Create_TypeTooLong_Throws() =>
        Should.Throw<ArgumentException>(() =>
            SystemEvent.Create(new string('a', SystemEvent.TypeMaxLength + 1), EventLevel.Info, "m", Now, null));
}
