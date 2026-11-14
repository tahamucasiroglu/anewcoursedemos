using System.Text;
using Shouldly;
using TahaKonsept.Agent.Engines.Speech.Supertonic;
using Xunit;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Supertonic;

/// <summary>Supertonic metin ön işleme, kodlama ve parçalama testleri.</summary>
public sealed class SupertonicTextTests
{
    /// <summary>Metin dil etiketiyle sarılır, sona nokta eklenir, boşluklar sadeleşir, tırnaklar düzleşir.</summary>
    [Fact]
    public void Preprocess_wraps_language_and_normalizes_punctuation()
    {
        var result = SupertonicTextProcessor.Preprocess("  Merhaba ,  “dünya”   — nasılsın  ", "tr");

        result.ShouldBe("<tr>Merhaba, \"dünya\" - nasılsın.</tr>".Normalize(NormalizationForm.FormKD));
    }

    /// <summary>Türkçe harfler NFKD ile ayrışır (ş → s + U+0327), emoji atılır, var olan noktalama korunur.</summary>
    [Fact]
    public void Preprocess_decomposes_turkish_letters_and_drops_emoji()
    {
        var result = SupertonicTextProcessor.Preprocess("Şu an 😀 iyi!", "tr");

        result.ShouldBe("<tr>Şu an iyi!</tr>");
    }

    /// <summary>Kodlama sözlükte olmayan karakterleri atar, diğerlerini eşler.</summary>
    [Fact]
    public void Encode_maps_known_characters_and_drops_unknown()
    {
        var indexer = Enumerable.Repeat(-1L, 65536).ToArray();
        foreach (var ch in "<tr>/a.")
        {
            indexer[ch] = ch;
        }

        var processor = new SupertonicTextProcessor(indexer);
        var ids = processor.Encode("ab", "tr");

        new string(ids.Select(i => (char)i).ToArray()).ShouldBe("<tr>a.</tr>");
    }

    /// <summary>Uzun metin cümle sınırlarından, sınırı aşmadan bölünür; kısaltmada bölünmez.</summary>
    [Fact]
    public void Chunker_splits_on_sentences_within_limit()
    {
        var text = "Birinci cümle burada. İkinci cümle de burada! Dr. Ahmet geldi mi? Son cümle.";

        var chunks = SupertonicTextChunker.Chunk(text, 30);

        chunks.ShouldAllBe(c => c.Length <= 30);
        chunks.Count.ShouldBe(3);
        chunks.ShouldContain(c => c.StartsWith("Dr. Ahmet geldi mi?", StringComparison.Ordinal));
        string.Join(' ', chunks).ShouldBe(text);
    }

    /// <summary>Kısa metin tek parça kalır; paragraflar ayrılır.</summary>
    [Fact]
    public void Chunker_keeps_short_text_and_splits_paragraphs()
    {
        SupertonicTextChunker.Chunk("Kısa metin.").ShouldBe(["Kısa metin."]);
        SupertonicTextChunker.Chunk("Bir.\n\nİki.").ShouldBe(["Bir.", "İki."]);
    }
}
