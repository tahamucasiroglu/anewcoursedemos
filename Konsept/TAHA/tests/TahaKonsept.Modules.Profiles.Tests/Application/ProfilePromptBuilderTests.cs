using Shouldly;
using TahaKonsept.Modules.Profiles.Application.Concrete;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Profiles;
using Xunit;

namespace TahaKonsept.Modules.Profiles.Tests.Application;

/// <summary>
/// <c>ProfilePromptBuilder</c> testleri: beklenen satırlar, boş alanların atlanması, boşluk sıkıştırma, kısaltma, dil/biçim/süre metinleri.
/// </summary>
public sealed class ProfilePromptBuilderTests
{
    private readonly ProfilePromptBuilder _builder = new();

    /// <summary>Tam profil beklenen kısa metni üretir.</summary>
    [Fact]
    public void Builds_expected_fragment_for_full_profile()
    {
        var profile = new ProfileDto
        {
            Name = "Bizans Günlükleri",
            Language = "tr",
            Format = VideoFormat.Vertical,
            TargetDurationSec = 60,
            ChannelDescription = "330–1453 Bizans.\n\n  Saray entrikaları   ve savaşlar.",
            DefaultTone = "merak uyandıran",
            DefaultAudience = "genel izleyici",
            DefaultNarrativeStyle = "hikâye",
        };

        var text = _builder.Build(profile);

        text.ShouldBe(
            "Kanal: Bizans Günlükleri\n" +
            "Dil: Türkçe (tr); tüm metin ve seslendirme bu dilde.\n" +
            "Biçim: dikey 9:16; hedef ~60 sn.\n" +
            "Odak: 330–1453 Bizans. Saray entrikaları ve savaşlar.\n" +
            "Ton: merak uyandıran | Kitle: genel izleyici | Anlatım: hikâye");
    }

    /// <summary>Boş opsiyonel alanlar ve boş içerik yazılmaz.</summary>
    [Fact]
    public void Omits_empty_fields()
    {
        var profile = new ProfileDto { Name = "Kanal", Language = "en", Format = VideoFormat.Horizontal, TargetDurationSec = 180, DefaultTone = "  " };

        var text = _builder.Build(profile);

        text.ShouldBe("Kanal: Kanal\nDil: İngilizce (en); tüm metin ve seslendirme bu dilde.\nBiçim: yatay 16:9; hedef ~3 dk.");
        text.ShouldNotContain("Odak:");
        text.ShouldNotContain("Ton:");
    }

    /// <summary>Uzun kanal içeriği kelime sınırında kesilir ve "…" ile biter.</summary>
    [Fact]
    public void Truncates_long_description_at_word_boundary()
    {
        var longText = string.Join(' ', Enumerable.Repeat("kelime", 400));
        var text = _builder.Build(new ProfileDto { Name = "K", ChannelDescription = longText });

        var focus = text.Split('\n').Single(l => l.StartsWith("Odak: ", StringComparison.Ordinal));
        focus.Length.ShouldBeLessThanOrEqualTo("Odak: ".Length + 1201);
        focus.ShouldEndWith("kelime…");
    }

    /// <summary>Bilinmeyen dil kodu olduğu gibi, süre dakika+saniye olarak yazılır.</summary>
    [Fact]
    public void Describes_unknown_language_and_mixed_duration()
    {
        var text = _builder.Build(new ProfileDto { Name = "K", Language = "sw", TargetDurationSec = 150 });

        text.ShouldContain("Dil: sw;");
        text.ShouldContain("hedef ~2 dk 30 sn.");
    }

    /// <summary>Çıktı token-optimize: satır sonu yok, çift boşluk yok.</summary>
    [Fact]
    public void Output_is_compact()
    {
        var text = _builder.Build(new ProfileDto { Name = "  A   B ", ChannelDescription = "x\t\ty\r\n\r\nz" });

        text.ShouldNotContain("  ");
        text.ShouldNotEndWith("\n");
        text.ShouldStartWith("Kanal: A B\n");
        text.ShouldContain("Odak: x y z");
    }
}
