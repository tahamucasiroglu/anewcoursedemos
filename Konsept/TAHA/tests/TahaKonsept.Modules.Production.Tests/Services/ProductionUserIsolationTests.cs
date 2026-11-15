using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Tests.Support;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Modules.Production.Tests.Services;

/// <summary>
/// Kullanıcı ayrımı (A ve B): A'nın videosu, sıra öğesi, işi, okunuş sözlüğü ve üretim ayarları B için yoktur. B'nin listeleri boş;
/// getir/sil/aç/yeni sürüm/yeniden üret/taşı/çıkar NotFound döner (varlık sızdırılmaz) ve A'nın kayıtları değişmeden kalır.
/// </summary>
public sealed class ProductionUserIsolationTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    /// <summary>B, A'nın ürettiği videoyu, sıra öğesini ve işi göremez/değiştiremez/silemez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Other_user_cannot_see_or_modify_videos_queue_or_jobs()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.LocalSpeech);
        await h.EnqueueSampleAsync();
        await h.WithAsync<IProductionQueueService, bool>(async s => (await s.StartAsync(None)).IsSuccess);
        await h.RunUntilIdleAsync();
        var video = (await h.VideosAsync()).Single();
        var job = (await h.JobsAsync()).Single();
        var item = (await h.StateAsync()).Items.Single();

        h.User.Current = Guid.CreateVersion7();

        (await h.WithAsync<IVideoService, Result<IReadOnlyList<VideoSummaryDto>>>(s => s.ListAsync(100, None))).Value.ShouldBeEmpty();
        (await h.WithAsync<IVideoService, Result<VideoDetailDto>>(s => s.GetAsync(video.Id, None))).Error.ShouldBe(ProductionErrors.VideoNotFound);
        (await h.WithAsync<IVideoService, Result>(s => s.OpenFolderAsync(video.Id, None))).Error.ShouldBe(ProductionErrors.VideoNotFound);
        (await h.WithAsync<IVideoService, Result>(s => s.OpenInPlayerAsync(video.Id, None))).Error.ShouldBe(ProductionErrors.VideoNotFound);
        (await h.WithAsync<IVideoService, Result<QueueItemDto>>(s => s.NewVersionAsync(video.Id, new NewVersionRequest(), None))).Error.ShouldBe(ProductionErrors.VideoNotFound);
        (await h.WithAsync<IVideoService, Result<QueueItemDto>>(s => s.RegenerateAsync(video.Id, new RegenerateRequest(), None))).Error.ShouldBe(ProductionErrors.VideoNotFound);
        (await h.WithAsync<IVideoService, Result>(s => s.DeleteAsync(video.Id, None))).Error.ShouldBe(ProductionErrors.VideoNotFound);
        h.Agent.DeletedVideos.ShouldBeEmpty();

        (await h.StateAsync()).Items.ShouldBeEmpty();
        (await h.WithAsync<IProductionQueueService, Result<QueueItemDto>>(s => s.GetItemAsync(item.Id, None))).Error.ShouldBe(ProductionErrors.QueueItemNotFound);
        (await h.WithAsync<IProductionQueueService, Result<QueueStateDto>>(s => s.MoveAsync(item.Id, new MoveQueueItemRequest { Direction = 1 }, None))).Error.ShouldBe(ProductionErrors.QueueItemNotFound);
        (await h.WithAsync<IProductionQueueService, Result>(s => s.RemoveAsync(item.Id, None))).Error.ShouldBe(ProductionErrors.QueueItemNotFound);
        (await h.WithAsync<IProductionQueueService, Result<QueueStateDto>>(s => s.ClearFinishedAsync(None))).Value.Items.ShouldBeEmpty();
        (await h.WithAsync<IJobQueryService, Result<JobDetailDto>>(s => s.GetAsync(job.Id, None))).Error.ShouldBe(ProductionErrors.JobNotFound);

        h.User.Current = h.UserId;
        (await h.VideosAsync()).ShouldHaveSingleItem().Id.ShouldBe(video.Id);
        (await h.StateAsync()).Items.ShouldHaveSingleItem().Id.ShouldBe(item.Id);
        (await h.WithAsync<IJobQueryService, Result<JobDetailDto>>(s => s.GetAsync(job.Id, None))).IsSuccess.ShouldBeTrue();
    }

    /// <summary>Okunuş sözlüğü ve üretim ayarları kullanıcıya özeldir: B, A'nın kaydını göremez, güncelleyemez, silemez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pronunciations_and_settings_are_per_user()
    {
        await using var h = new ProductionTestHarness();
        await h.SaveSettingsAsync(TestSettings.OnlineMedia);
        var entry = (await h.WithAsync<IPronunciationService, Result<PronunciationDto>>(s =>
            s.SaveAsync(new SavePronunciationRequest { Written = "Google", Spoken = "Gugıl" }, None))).Value;

        h.User.Current = Guid.CreateVersion7();

        (await h.WithAsync<IPronunciationService, Result<IReadOnlyList<PronunciationDto>>>(s => s.ListAsync(None))).Value.ShouldBeEmpty();
        (await h.WithAsync<IPronunciationService, Result>(s => s.DeleteAsync(entry.Id, None))).Error.ShouldBe(ProductionErrors.PronunciationNotFound);
        (await h.WithAsync<IPronunciationService, Result<PronunciationDto>>(s =>
            s.SaveAsync(new SavePronunciationRequest { Id = entry.Id, Written = "Google", Spoken = "Gogle" }, None))).Error.ShouldBe(ProductionErrors.PronunciationNotFound);
        var own = (await h.WithAsync<IPronunciationService, Result<PronunciationDto>>(s =>
            s.SaveAsync(new SavePronunciationRequest { Written = "Google", Spoken = "Guugıl" }, None))).Value;
        own.Id.ShouldNotBe(entry.Id);
        (await h.WithAsync<IProductionSettingsService, Result<ProductionSettingsDto>>(s => s.GetAsync(None))).Value.SpeechModelKey
            .ShouldNotBe(TestSettings.OnlineMedia.SpeechModelKey);

        h.User.Current = h.UserId;
        (await h.WithAsync<IPronunciationService, Result<IReadOnlyList<PronunciationDto>>>(s => s.ListAsync(None))).Value
            .ShouldHaveSingleItem().Spoken.ShouldBe("Gugıl");
        (await h.WithAsync<IProductionSettingsService, Result<ProductionSettingsDto>>(s => s.GetAsync(None))).Value.SpeechModelKey
            .ShouldBe(TestSettings.OnlineMedia.SpeechModelKey);
    }
}
