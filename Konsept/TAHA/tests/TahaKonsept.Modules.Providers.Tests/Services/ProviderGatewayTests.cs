using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Concrete;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.TextModels.Abstract;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.Services;

/// <summary>
/// Gateway: model/anahtar doğrulaması, çıkış sınırı, akış eşiği, web araması desteği.
/// </summary>
public sealed class ProviderGatewayTests
{
    private readonly IApiKeyVault _vault = Substitute.For<IApiKeyVault>();
    private readonly ITextModelFactory _factory = Substitute.For<ITextModelFactory>();
    private readonly ITextModel _model = Substitute.For<ITextModel>();
    private readonly ProviderGateway _gateway;
    private TextRequest? _captured;

    /// <summary>Kurucu.</summary>
    public ProviderGatewayTests()
    {
        _gateway = new ProviderGateway(TestFixtures.Catalog, _vault, _factory, new FixedClock(), NullLogger<ProviderGateway>.Instance);
        _vault.GetCredentialAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(c => Result.Success(new ProviderCredential(c.ArgAt<string>(1), "k-12345678", null)));
        _factory.Create(Arg.Any<ModelInfo>(), Arg.Any<ProviderCredential>()).Returns(Result.Success(_model));
        _model.RunTextAsync(Arg.Do<TextRequest>(r => _captured = r), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new TextResult<string> { Value = "ok" }));
    }

    /// <summary>Video kodu: 64000 token → akış açık, efor ve önbellek taşınır.</summary>
    [Fact]
    public async Task Long_output_enables_streaming()
    {
        var result = await _gateway.RunTextAsync(Task("claude-opus-5-5") with { MaxOutputTokens = 64000, Effort = EffortLevel.High }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _captured.ShouldNotBeNull();
        _captured.MaxOutputTokens.ShouldBe(64000);
        _captured.Stream.ShouldBeTrue();
        _captured.Effort.ShouldBe(EffortLevel.High);
        _captured.CacheSystemPrompt.ShouldBeTrue();
        _captured.Context.Purpose.ShouldBe("scene-code");
    }

    /// <summary>Varsayılan çıkış sınırı 16000 (akış yok); web araması desteklenmeyen modelde kapatılır.</summary>
    [Fact]
    public async Task Defaults_and_web_search_support()
    {
        await _gateway.RunTextAsync(Task("gpt-6.1-sol") with { UseWebSearch = true }, TestContext.Current.CancellationToken);
        _captured!.MaxOutputTokens.ShouldBe(16000);
        _captured.Stream.ShouldBeFalse();
        _captured.UseWebSearch.ShouldBeFalse();

        await _gateway.RunTextAsync(Task("claude-sonnet-5-5") with { UseWebSearch = true, MaxOutputTokens = 500000 }, TestContext.Current.CancellationToken);
        _captured.UseWebSearch.ShouldBeTrue();
        _captured.MaxOutputTokens.ShouldBe(128000);
    }

    /// <summary>Bilinmeyen model, yerel model, anahtar eksikliği ve boş prompt hataları.</summary>
    [Fact]
    public async Task Validation_errors()
    {
        var ct = TestContext.Current.CancellationToken;
        (await _gateway.RunTextAsync(Task("yok-boyle-model"), ct)).Error.Code.ShouldBe(ProviderErrorCodes.ModelNotFound);
        (await _gateway.RunTextAsync(Task("supertonic-3"), ct)).Error.Code.ShouldBe(ProviderErrorCodes.ModelNotSupported);
        (await _gateway.RunTextAsync(Task("claude-haiku-5-5") with { UserPrompt = " " }, ct)).Error.Code.ShouldBe(ProviderErrorCodes.InvalidRequest);

        _vault.GetCredentialAsync(Arg.Any<Guid>(), ProviderPlatforms.Zai, Arg.Any<CancellationToken>())
            .Returns(Error.Validation(ProviderErrorCodes.KeyMissing, "yok"));
        (await _gateway.RunTextAsync(Task("glm-5.3"), ct)).Error.Code.ShouldBe(ProviderErrorCodes.KeyMissing);
    }

    /// <summary>Örnek iş.</summary>
    /// <param name="modelKey">Model.</param>
    private static TextTask Task(string modelKey) => new()
    {
        UserId = Guid.CreateVersion7(),
        ModelKey = modelKey,
        SystemPrompt = "sistem",
        UserPrompt = "kullanıcı",
        Purpose = "scene-code",
    };
}
