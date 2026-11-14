using Shouldly;
using TahaKonsept.Modules.Concepts.Application.Concrete;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// <see cref="TurkishTopicNormalizer"/>: büyük/küçük harf, aksan, kesme işaretli ek ve dolgu kelimesi farkları aynı konuyu verir.
/// </summary>
public sealed class TopicNormalizerTests
{
    private readonly TurkishTopicNormalizer _n = new();

    /// <summary>Yazım farkları aynı parmak izini verir.</summary>
    [Fact]
    public void Spelling_variants_have_same_fingerprint()
    {
        var a = _n.Fingerprint("Bizans'ın Son İmparatoru");
        var b = _n.Fingerprint("bizansin son imparatoru");
        var c = _n.Fingerprint("BİZANS SON İMPARATORU KİMDİ?");
        a.Hash.ShouldBe(b.Hash);
        _n.IsDuplicate(a, c).ShouldBeTrue();
    }

    /// <summary>Kelime sırası ve küçük ek farkı tekrar sayılır; farklı konu sayılmaz.</summary>
    [Fact]
    public void Similar_titles_are_duplicates_but_different_topics_are_not()
    {
        var a = _n.Fingerprint("İstanbul'un Fethinde Şahi Topları");
        var b = _n.Fingerprint("Şahi Topları ve İstanbul'un Fethi");
        var c = _n.Fingerprint("Malazgirt Savaşı'nda Romen Diyojen");
        _n.IsDuplicate(a, b).ShouldBeTrue();
        _n.IsDuplicate(a, c).ShouldBeFalse();
        _n.Similarity(a, c).ShouldBe(0);
    }

    /// <summary>Saklanan anahtar kelimelerden kurulan iz aynıdır.</summary>
    [Fact]
    public void From_stored_round_trips()
    {
        var a = _n.Fingerprint("Ayasofya'nın Kubbesi Neden Çöktü");
        var b = _n.FromStored(a.Keywords, a.Hash);
        _n.IsDuplicate(a, b).ShouldBeTrue();
        b.Words.Count.ShouldBe(a.Words.Count);
    }
}
