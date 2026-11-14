using NSubstitute;
using Shouldly;
using TahaKonsept.Modules.Concepts.Application.Concrete;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Anlatım hızı: bağlam kurucu, üretimdeki ses modelinin hızını (port) kullanır; port bilgi vermezse dil sabitine düşer;
/// hedef kelime sayısı bu hızla hesaplanır; yedek port <c>null</c> döner, aralık dışı değer sıkıştırılır.
/// </summary>
public sealed class NarrationPaceTests : IDisposable
{
    private readonly ConceptsTestContext _ctx = new();

    /// <summary>Port 1,7 verirse 20 sn hedef ≈ 34 kelime olur.</summary>
    [Fact]
    public async Task Context_uses_speech_model_rate_from_port()
    {
        _ctx.Pace.GetWordsPerSecondAsync(_ctx.UserId, "tr", Arg.Any<CancellationToken>()).Returns(Task.FromResult<double?>(1.7));
        _ctx.Profile = _ctx.Profile with { TargetDurationSec = 20 };

        var context = (await _ctx.Contexts.ResolveAsync(_ctx.UserId, null, TestContext.Current.CancellationToken)).Value;

        context.WordsPerSecond.ShouldBe(1.7);
        context.TargetWords.ShouldBe(34);
    }

    /// <summary>Port bilgi vermezse Türkçe yedek sabit kullanılır.</summary>
    [Fact]
    public async Task Context_falls_back_to_language_constant()
    {
        var context = (await _ctx.Contexts.ResolveAsync(_ctx.UserId, null, TestContext.Current.CancellationToken)).Value;

        context.WordsPerSecond.ShouldBe(ConceptLimits.TurkishWordsPerSecond);
    }

    /// <summary>Yedek port bilgi vermez.</summary>
    [Fact]
    public async Task Default_source_returns_null()
    {
        var value = await new DefaultNarrationPaceSource().GetWordsPerSecondAsync(Guid.NewGuid(), "tr", TestContext.Current.CancellationToken);

        value.ShouldBeNull();
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();
}
