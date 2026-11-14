using Shouldly;
using TahaKonsept.Modules.Concepts.Domain.Entities;
using TahaKonsept.Modules.Concepts.Domain.Enums;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Domain;

/// <summary>
/// <see cref="TopicMemoryEntry"/> tür güçlendirme kuralları.
/// </summary>
public sealed class TopicMemoryEntryTests
{
    /// <summary>Tür yalnız güçlenir; Demote açıkça zayıflatır; negatiflik Deleted/Disliked'ta.</summary>
    [Fact]
    public void Promote_only_strengthens()
    {
        var e = TopicMemoryEntry.Create(Guid.NewGuid(), null, "Başlık", "baslik", "h", TopicMemoryKind.Generated);
        e.Promote(TopicMemoryKind.Suggested).ShouldBeFalse();
        e.Kind.ShouldBe(TopicMemoryKind.Generated);
        e.Promote(TopicMemoryKind.Deleted).ShouldBeTrue();
        e.IsNegative.ShouldBeTrue();
        e.Demote(TopicMemoryKind.Generated);
        e.IsNegative.ShouldBeFalse();
    }
}
