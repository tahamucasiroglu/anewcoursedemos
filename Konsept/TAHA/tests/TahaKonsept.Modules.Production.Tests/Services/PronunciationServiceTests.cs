using Shouldly;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Tests.Support;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Services;

/// <summary>
/// Okunuş sözlüğü servisi: ekle / aynı yazılışı güncelle / öneriyi onayla / sil, önizleme (yerel → ajan adresi, online → data: adresi, kapalı → mesaj).
/// </summary>
public sealed class PronunciationServiceTests
{
    /// <summary>Aynı yazılış tek kayıt; öneri onaylanınca kalıcı olur; silinebilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Save_updates_same_written_and_approves_suggestion()
    {
        await using var h = new ProductionTestHarness();
        var suggestion = await Save(h, new SavePronunciationRequest { Written = "NVIDIA", Spoken = "Enviya", IsSuggestion = true });
        suggestion.IsSuggestion.ShouldBeTrue();

        var approved = await Save(h, new SavePronunciationRequest { Written = "nvıdıa".Replace("ı", "i", StringComparison.Ordinal).ToUpperInvariant(), Spoken = "Enviıdiya" });
        approved.Id.ShouldBe(suggestion.Id);
        approved.Status.ShouldBe(PronunciationStatus.Approved);

        var list = await h.WithAsync<IPronunciationService, IReadOnlyList<PronunciationDto>>(async s => (await s.ListAsync(CancellationToken.None)).Value);
        list.Single().Spoken.ShouldBe("Enviıdiya");

        (await h.WithAsync<IPronunciationService, bool>(async s => (await s.DeleteAsync(approved.Id, CancellationToken.None)).IsSuccess)).ShouldBeTrue();
        (await h.WithAsync<IPronunciationService, IReadOnlyList<PronunciationDto>>(async s => (await s.ListAsync(CancellationToken.None)).Value)).ShouldBeEmpty();
    }

    /// <summary>Yerel seste önizleme ajandan gelir; okunuş sözlükle düzeltilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Preview_local_uses_agent_and_dictionary()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await Save(h, new SavePronunciationRequest { Written = "YouTube", Spoken = "Yutüb" });

        var preview = await Preview(h, "YouTube kanalı");

        preview.Source.ShouldBe("agent");
        preview.SpokenText.ShouldContain("Yutüb");
        preview.AudioUrl.ShouldNotBeNull().ShouldStartWith("http://192.168.1.10:7120/previews/");
        h.Agent.Previews.Single().EngineId.ShouldBe("supertonic-3");
    }

    /// <summary>Online seste önizleme sağlayıcıdan base64 olarak döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Preview_online_returns_data_url()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.OnlineMedia);

        var preview = await Preview(h, "Merhaba");

        preview.Source.ShouldBe("online");
        preview.AudioBase64.ShouldNotBeNullOrEmpty();
        preview.AudioUrl.ShouldNotBeNull().ShouldStartWith("data:audio/wav;base64,");
    }

    /// <summary>Ses kapalıyken önizleme açıklama döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Preview_with_speech_off_explains()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech with { SpeechMode = MediaMode.Off });

        var preview = await Preview(h, "Merhaba");

        preview.Source.ShouldBe("none");
        preview.Message.ShouldNotBeNull().ShouldContain("Ses kapalı");
    }

    private static Task<PronunciationDto> Save(ProductionTestHarness h, SavePronunciationRequest request) =>
        h.WithAsync<IPronunciationService, PronunciationDto>(async s => (await s.SaveAsync(request, CancellationToken.None)).Value);

    private static Task<PronunciationPreviewResponse> Preview(ProductionTestHarness h, string text) =>
        h.WithAsync<IPronunciationService, PronunciationPreviewResponse>(async s => (await s.PreviewAsync(new PronunciationPreviewRequest { Text = text }, CancellationToken.None)).Value);
}
