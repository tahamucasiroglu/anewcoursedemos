using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Modules.Production.Infrastructure.Integration.Concrete;
using TahaKonsept.Modules.Production.Tests.Support;
using TahaKonsept.Shared.Contracts.Diagnostics;
using TahaKonsept.Shared.Contracts.Production;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Services;

/// <summary>
/// Konuşma hızı: katalog hızı × hız çarpanı, ses kapalı / Türkçe dışı → bilgi yok, bilinmeyen model → yerel/online varsayılan,
/// ölçüm kaydı ve ortalamanın tercih edilmesi (ses başına), makul olmayan ölçümün atılması, örnek ağırlığı sınırı,
/// video bitince otomatik ölçüm, Concepts adaptörü ve ScenePlan'ın %15 aşım uyarısı.
/// </summary>
public sealed class SpeechRateTests
{
    private static ProductionTestHarness Harness()
    {
        var h = new ProductionTestHarness();
        h.Catalog.Find("supertonic-3").Returns(new ModelInfo { Key = "supertonic-3", Capability = ModelCapability.Speech, RunsOn = ModelRunsOn.Agent, WordsPerSecond = 1.7 });
        h.Catalog.Find("online-x").Returns(new ModelInfo { Key = "online-x", Capability = ModelCapability.Speech, RunsOn = ModelRunsOn.Server });
        return h;
    }

    private static Task<double?> RateAsync(ProductionTestHarness h, ProductionSettingsDto settings, string language = "tr") =>
        h.WithAsync<ISpeechRateService, double?>(s => s.GetWordsPerSecondAsync(h.UserId, settings, language, CancellationToken.None));

    /// <summary>Katalog hızı (Supertonic 1,7) hız çarpanıyla çarpılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Uses_catalog_rate_times_speed()
    {
        await using var h = Harness();

        (await RateAsync(h, TestSettings.LocalSpeech)).ShouldBe(1.7);
        (await RateAsync(h, TestSettings.LocalSpeech with { SpeechSpeed = 1.5 })).ShouldBe(2.55);
    }

    /// <summary>Ses kapalıysa, model yoksa ya da dil Türkçe değilse (ölçüm yokken) bilgi yok.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Returns_null_when_silent_or_unknown_language()
    {
        await using var h = Harness();

        (await RateAsync(h, TestSettings.LocalSpeech with { SpeechMode = MediaMode.Off })).ShouldBeNull();
        (await RateAsync(h, TestSettings.LocalSpeech with { SpeechModelKey = null })).ShouldBeNull();
        (await RateAsync(h, TestSettings.LocalSpeech, "en")).ShouldBeNull();
    }

    /// <summary>Katalogda hızı olmayan model: yerel 2,0, online 2,2.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Unknown_rate_uses_local_or_online_default()
    {
        await using var h = Harness();

        (await RateAsync(h, TestSettings.LocalSpeech with { SpeechModelKey = "yok-boyle-model" })).ShouldBe(2.0);
        (await RateAsync(h, TestSettings.OnlineMedia with { SpeechModelKey = "online-x" })).ShouldBe(2.2);
    }

    /// <summary>
    /// Ölçüm (e2e: 50 kelime / 29,23 sn ≈ 1,71) kaydedilince o model + ses için ortalama kullanılır; başka ses katalog hızında kalır;
    /// 1,25× hızda ölçülen süre 1.0×'a indirgenir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Recorded_average_is_preferred_per_voice()
    {
        await using var h = Harness();
        var settings = TestSettings.LocalSpeech with { SpeechModelKey = "chatterbox-multilingual", VoiceId = "M1" };

        var average = await h.WithAsync<ISpeechRateService, double?>(s => s.RecordAsync(h.UserId, settings, "tr", 50, 29.23, CancellationToken.None));
        average.ShouldNotBeNull();
        average.Value.ShouldBe(1.711, 0.001);

        (await RateAsync(h, settings)).ShouldBe(1.71);
        (await RateAsync(h, settings with { VoiceId = "F1" })).ShouldBe(2.0);

        var fast = settings with { SpeechSpeed = 1.25 };
        await h.WithAsync<ISpeechRateService, double?>(s => s.RecordAsync(h.UserId, fast, "tr", 50, 29.23 / 1.25, CancellationToken.None));
        (await RateAsync(h, settings)).ShouldBe(1.71);
        (await RateAsync(h, fast)).ShouldBe(2.14);
    }

    /// <summary>Makul olmayan (10 kelime/sn), çok kısa ya da sessiz ölçüm atılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Implausible_samples_are_ignored()
    {
        await using var h = Harness();

        (await h.WithAsync<ISpeechRateService, double?>(s => s.RecordAsync(h.UserId, TestSettings.LocalSpeech, "tr", 50, 5, CancellationToken.None))).ShouldBeNull();
        (await h.WithAsync<ISpeechRateService, double?>(s => s.RecordAsync(h.UserId, TestSettings.LocalSpeech, "tr", 4, 3, CancellationToken.None))).ShouldBeNull();
        (await h.WithAsync<ISpeechRateService, double?>(s => s.RecordAsync(h.UserId, TestSettings.LocalSpeech with { SpeechMode = MediaMode.Off }, "tr", 50, 25, CancellationToken.None))).ShouldBeNull();
        (await RateAsync(h, TestSettings.LocalSpeech)).ShouldBe(1.7);
    }

    /// <summary>Örnek ağırlığı sınırda kalır; yeni ölçümler ortalamayı kaydırır.</summary>
    [Fact]
    public void Sample_weight_is_capped_and_average_adapts()
    {
        var stat = SpeechRateStat.Create(Guid.NewGuid(), "supertonic-3", null, "tr");
        for (var i = 0; i < 30; i++)
        {
            stat.AddSample(20, 10);
        }

        stat.SampleWeight.ShouldBe(ProductionLimits.MaxSpeechRateSamples);
        stat.WordsPerSecond.ShouldBe(2.0, 0.0001);

        for (var i = 0; i < 40; i++)
        {
            stat.AddSample(10, 10);
        }

        stat.WordsPerSecond.ShouldBeLessThan(1.15);
    }

    /// <summary>Video bitince gerçek hız (25 kelime / 9 sn ≈ 2,78) kaydedilir; Concepts adaptörü bu hızı verir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Finished_video_calibrates_rate_and_concepts_adapter_uses_it()
    {
        await using var h = Harness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        await h.WithAsync<IProductionQueueService, bool>(async s => (await s.StartAsync(CancellationToken.None)).IsSuccess);
        await h.RunUntilIdleAsync();
        (await h.VideosAsync()).Count.ShouldBe(1);

        (await h.WithAsync<ISpeechRateService, double?>(s => s.GetForUserAsync(h.UserId, "tr", CancellationToken.None))).ShouldBe(2.78);

        var pace = await h.InScopeAsync<double?>(sp =>
            new ProductionNarrationPaceSource(sp.GetRequiredService<ISpeechRateService>()).GetWordsPerSecondAsync(h.UserId, "tr", CancellationToken.None));
        pace.ShouldBe(2.78);
    }

    /// <summary>Hedef 10 sn, 25 kelime / 1,7 ≈ 15 sn (%47 fazla) → sarı uyarı; hedef 30 sn → uyarı yok.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Scene_plan_warns_when_speech_exceeds_target()
    {
        await using var h = Harness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        var shortId = h.Concepts.Add(FakeConceptSource.Sample("Kısa hedef") with { TargetDurationSec = 10 });
        var okId = h.Concepts.Add(FakeConceptSource.Sample("Uygun hedef"));
        await h.WithAsync<IProductionQueueService, bool>(async s => (await s.AddAsync(new AddToQueueRequest { ConceptIds = [shortId, okId] }, CancellationToken.None)).IsSuccess);
        await h.WithAsync<IProductionQueueService, bool>(async s => (await s.StartAsync(CancellationToken.None)).IsSuccess);
        await h.RunUntilIdleAsync();

        var warnings = h.Activity.Entries.Where(e => e.Level == LogLevelKind.Warning && e.Message.StartsWith("Seslendirme profil hedefini aşıyor", StringComparison.Ordinal)).ToList();
        warnings.Count.ShouldBe(1);
        warnings[0].Message.ShouldContain("hedef 10 sn");
        warnings[0].Message.ShouldContain("Kısa hedef");
        (await h.VideosAsync()).Count.ShouldBe(2);
    }
}
