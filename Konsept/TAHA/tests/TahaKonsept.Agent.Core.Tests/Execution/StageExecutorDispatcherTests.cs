using System.Text.Json;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Align;
using TahaKonsept.Agent.Abstractions.Align.Abstract;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Abstractions.Image.Abstract;
using TahaKonsept.Agent.Abstractions.Speech;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Abstractions.Video;
using TahaKonsept.Agent.Abstractions.Video.Abstract;
using TahaKonsept.Agent.Core.Audio;
using TahaKonsept.Agent.Core.Audio.Concrete;
using TahaKonsept.Agent.Core.Execution;
using TahaKonsept.Agent.Core.Execution.Concrete;
using TahaKonsept.Agent.Core.Media.Abstract;
using TahaKonsept.Agent.Core.Tests.Support;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Common;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Execution;

/// <summary>
/// Aşama dağıtımı ve yürütücüler (sahte motorlarla): doğru yürütücü seçimi, girdi doğrulama, kontrol noktası (var olan çıktı atlanır),
/// girdi hash'i değişince yeniden üretim, ses birleştirme ve zamanlama, sahne lint hatası, hizalama çıktısı.
/// </summary>
public sealed class StageExecutorDispatcherTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly IImageEngine _image = Substitute.For<IImageEngine>();
    private readonly ISpeechEngine _speech = Substitute.For<ISpeechEngine>();
    private readonly IVideoEngine _video = Substitute.For<IVideoEngine>();
    private readonly IAligner _aligner = Substitute.For<IAligner>();
    private readonly StageExecutorDispatcher _dispatcher;

    /// <summary>Sahte motorları ve dağıtıcıyı kurar.</summary>
    public StageExecutorDispatcherTests()
    {
        _image.Key.Returns("sdcpp");
        _image.DisplayName.Returns("sd.cpp");
        _image.SupportedModels.Returns(["z-image-turbo"]);
        _image.IsInstalled().Returns(true);
        _image.GenerateAsync(Arg.Any<ImageRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<ImageRequest>();
            File.WriteAllBytes(request.OutputPath, [1, 2, 3, 4]);
            return Task.FromResult(Result.Success(new ImageOutput(request.OutputPath)));
        });

        _speech.Key.Returns("supertonic-3");
        _speech.DisplayName.Returns("Supertonic");
        _speech.IsInstalled().Returns(true);
        _speech.SupportsLanguage(Arg.Any<string?>()).Returns(true);
        _speech.SynthesizeAsync(Arg.Any<SpeechRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<SpeechRequest>();
            WavFile.WriteSilence(request.OutputPath, 1.0);
            return Task.FromResult(Result.Success(new SpeechOutput(request.OutputPath, 1.0)));
        });

        _video.Key.Returns("hyperframes");
        _video.DisplayName.Returns("HyperFrames");
        _video.IsInstalled().Returns(true);

        _aligner.Key.Returns("proportional");

        _dispatcher = new StageExecutorDispatcher(
        [
            new ImagesStageExecutor([_image]),
            new SpeechStageExecutor([_speech], new WavAudioConcatenator(Substitute.For<IMediaProbe>())),
            new SceneRenderStageExecutor([_video]),
            new AlignStageExecutor([_aligner]),
        ]);
    }

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>Ajanda yürütücüsü olmayan aşama <c>agent.stage_not_supported</c> döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Dispatch_UnknownStage_ReturnsNotSupported()
    {
        _dispatcher.CanExecute(PipelineStage.Script).ShouldBeFalse();
        _dispatcher.CanExecute(PipelineStage.Images).ShouldBeTrue();

        var result = await _dispatcher.DispatchAsync(Context(PipelineStage.Script, new { }), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("agent.stage_not_supported");
    }

    /// <summary>Bozuk JSON <c>agent.invalid_payload</c> döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Dispatch_InvalidJson_ReturnsInvalidPayload()
    {
        var command = new StageCommand { CommandId = Guid.NewGuid(), JobId = Guid.NewGuid(), Stage = PipelineStage.Images, PayloadJson = "{bozuk" };

        var result = await _dispatcher.DispatchAsync(new StageExecutionContext(command, _temp.Path, null, null), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("agent.invalid_payload");
    }

    /// <summary>Görseller üretilir; ikinci çalıştırmada var olanlar atlanır; hash değişince yeniden üretilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Images_GeneratesThenSkipsExisting_AndRegeneratesOnHashChange()
    {
        var ct = TestContext.Current.CancellationToken;
        var payload = new ImagesStagePayload
        {
            ModelKey = "z-image-turbo",
            Items =
            [
                new ImagePromptItem { SceneIndex = 0, Name = "scene-0-1.png", Prompt = "kale", Seed = 1 },
                new ImagePromptItem { SceneIndex = 1, Name = "../kaçış.png", Prompt = "deniz", Seed = 2 },
            ],
        };

        var first = await _dispatcher.DispatchAsync(Context(PipelineStage.Images, payload, "hash-a"), ct);
        var second = await _dispatcher.DispatchAsync(Context(PipelineStage.Images, payload, "hash-a"), ct);

        first.IsSuccess.ShouldBeTrue();
        first.Value.Artifacts.Count.ShouldBe(2);
        first.Value.Artifacts[0].LocalPath.ShouldBe("images/scene-0-1.png");
        first.Value.Artifacts[1].Name.ShouldBe("kaçış.png");
        first.Value.Artifacts[0].Sha256!.Length.ShouldBe(64);
        second.IsSuccess.ShouldBeTrue();
        await _image.Received(2).GenerateAsync(Arg.Any<ImageRequest>(), Arg.Any<CancellationToken>());
        Directory.GetFiles(Path.Combine(_temp.Path, "images"), "*.partial*").ShouldBeEmpty();

        var third = await _dispatcher.DispatchAsync(Context(PipelineStage.Images, payload, "hash-b"), ct);

        third.IsSuccess.ShouldBeTrue();
        await _image.Received(4).GenerateAsync(Arg.Any<ImageRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Modeli destekleyen motor yoksa <c>agent.engine_not_found</c>.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Images_UnknownModel_ReturnsEngineNotFound()
    {
        var payload = new ImagesStagePayload { ModelKey = "flux-yok", Items = [new ImagePromptItem { Name = "a.png", Prompt = "x" }] };

        var result = await _dispatcher.DispatchAsync(Context(PipelineStage.Images, payload), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("agent.engine_not_found");
    }

    /// <summary>Sahneler seslendirilir, boş metne sessizlik eklenir, birleşik ses ve sahne zamanlamaları döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Speech_SynthesizesScenes_CombinesAndReturnsTimings()
    {
        var payload = new SpeechStagePayload
        {
            EngineKey = "supertonic-3",
            Segments =
            [
                new SpeechSegmentItem { SceneIndex = 0, DisplayText = "Google", SpokenText = "Gugıl" },
                new SpeechSegmentItem { SceneIndex = 1, DisplayText = "", SpokenText = "" },
                new SpeechSegmentItem { SceneIndex = 2, DisplayText = "1453", SpokenText = "bin dört yüz elli üç" },
            ],
        };

        var result = await _dispatcher.DispatchAsync(Context(PipelineStage.Speech, payload), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        await _speech.Received(2).SynthesizeAsync(Arg.Any<SpeechRequest>(), Arg.Any<CancellationToken>());
        var output = JsonSerializer.Deserialize<SpeechStageResult>(result.Value.OutputJson!, ContractJson.Options)!;
        output.Scenes.Count.ShouldBe(3);
        output.Scenes[1].DurationSec.ShouldBe(SpeechStageExecutor.EmptySegmentSilenceSec, 0.01);
        output.Scenes[2].StartSec.ShouldBe(1.0 + SpeechStageExecutor.EmptySegmentSilenceSec, 0.01);
        output.TotalDurationSec.ShouldBe(2.0 + SpeechStageExecutor.EmptySegmentSilenceSec, 0.01);
        File.Exists(WorkspaceLayout.SpeechAudioPath(_temp.Path)).ShouldBeTrue();
        result.Value.Artifacts.Count.ShouldBe(4);
        await _speech.Received(1).ReleaseAsync();
    }

    /// <summary>
    /// Motor içeriğin dilini desteklemiyorsa (Kokoro + Türkçe) kurulum ve sentez denenmeden açık bir Türkçe hata döner.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Speech_UnsupportedLanguage_FailsBeforeInstall()
    {
        _speech.SupportsLanguage("tr-TR").Returns(false);
        _speech.DisplayName.Returns("Kokoro");
        _speech.LanguageLabel.Returns("Türkçe yok (İngilizce vb.)");
        var payload = new SpeechStagePayload
        {
            EngineKey = "supertonic-3",
            Language = "tr-TR",
            Segments = [new SpeechSegmentItem { SceneIndex = 0, DisplayText = "Google", SpokenText = "Gugıl" }],
        };

        var result = await _dispatcher.DispatchAsync(Context(PipelineStage.Speech, payload), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("speech.unsupported_language");
        result.Error.Message.ShouldContain("Kokoro Türkçe seslendiremez");
        _ = _speech.DidNotReceive().IsInstalled();
        await _speech.DidNotReceive().SynthesizeAsync(Arg.Any<SpeechRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Sentez başarısız olsa da ses aşaması sonunda motor bırakılır (GPU belleği boşalır).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Speech_ReleasesEngine_EvenWhenSynthesisFails()
    {
        _speech.SynthesizeAsync(Arg.Any<SpeechRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<SpeechOutput>(Error.Failure("speech.synthesis_failed", "CUDA out of memory"))));
        var payload = new SpeechStagePayload
        {
            EngineKey = "supertonic-3",
            Segments = [new SpeechSegmentItem { SceneIndex = 0, DisplayText = "a", SpokenText = "a" }],
        };

        var result = await _dispatcher.DispatchAsync(Context(PipelineStage.Speech, payload), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        await _speech.Received(1).ReleaseAsync();
    }

    /// <summary>Sahne kodu lint hatası <c>agent.scene_invalid</c> döner ve render çağrılmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SceneRender_ValidationFailure_ReturnsSceneInvalid()
    {
        _video.ValidateSceneAsync(Arg.Any<SceneRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Validation("hyperframes.lint", "3. satırda kapanmamış etiket"))));
        var payload = new SceneRenderStagePayload { EngineKey = "hyperframes", SceneCode = "<div>", DurationSec = 4 };

        var result = await _dispatcher.DispatchAsync(Context(PipelineStage.SceneRender, payload, scene: 2), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("agent.scene_invalid");
        result.Error.Message.ShouldContain("kapanmamış");
        await _video.DidNotReceive().RenderSceneAsync(Arg.Any<SceneRenderRequest>(), Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Geçerli sahne <c>scenes/&lt;i&gt;/scene-&lt;i&gt;.mp4</c> olarak render edilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SceneRender_RendersToSceneFolder()
    {
        _video.ValidateSceneAsync(Arg.Any<SceneRenderRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _video.RenderSceneAsync(Arg.Any<SceneRenderRequest>(), Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<SceneRenderRequest>();
            File.WriteAllBytes(request.OutputPath, [9, 9, 9]);
            return Task.FromResult(Result.Success(new VideoRenderOutput(request.OutputPath, 4.2)));
        });
        var payload = new SceneRenderStagePayload { EngineKey = "hyperframes", SceneCode = "<div></div>", DurationSec = 4 };

        var result = await _dispatcher.DispatchAsync(Context(PipelineStage.SceneRender, payload, scene: 3), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        File.Exists(WorkspaceLayout.SceneVideoPath(_temp.Path, 3)).ShouldBeTrue();
        result.Value.Artifacts.Single().DurationSec.ShouldBe(4.2);
        result.Value.Artifacts.Single().LocalPath.ShouldBe("scenes/3/scene-3.mp4");
    }

    /// <summary>Hizalama <c>align/captions.json</c> yazar ve kelime zamanlarını döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Align_WritesCaptions()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.Combine(_temp.Path, WorkspaceLayout.SpeechDir));
        WavFile.WriteSilence(WorkspaceLayout.SpeechAudioPath(_temp.Path), 1.0);
        _aligner.AlignAsync(Arg.Any<AlignRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<CaptionWord>>([new CaptionWord("Google", 0, 400), new CaptionWord("dedi", 400, 900)])));
        var payload = new AlignStagePayload { Segments = [new AlignSegmentItem { SceneIndex = 0, DisplayText = "Google dedi", SpokenText = "Gugıl dedi" }] };

        var result = await _dispatcher.DispatchAsync(Context(PipelineStage.Align, payload), ct);

        result.IsSuccess.ShouldBeTrue();
        var output = JsonSerializer.Deserialize<AlignStageResult>(result.Value.OutputJson!, ContractJson.Options)!;
        output.Words.Select(w => w.Text).ShouldBe(["Google", "dedi"]);
        AlignStageExecutor.ReadCaptionWords(WorkspaceLayout.CaptionsPath(_temp.Path)).Count.ShouldBe(2);
    }

    private StageExecutionContext Context(PipelineStage stage, object payload, string hash = "h1", int? scene = null) =>
        new(
            new StageCommand
            {
                CommandId = Guid.NewGuid(),
                JobId = Guid.NewGuid(),
                VideoId = Guid.NewGuid(),
                Stage = stage,
                SceneIndex = scene,
                InputHash = hash,
                PayloadJson = JsonSerializer.Serialize(payload, payload.GetType(), ContractJson.Options),
            },
            _temp.Path,
            null,
            null);
}
