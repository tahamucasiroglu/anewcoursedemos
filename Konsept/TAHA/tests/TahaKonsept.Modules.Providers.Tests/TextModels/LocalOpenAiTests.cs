using NSubstitute;
using Shouldly;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Concrete;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.Validators;
using TahaKonsept.Modules.Providers.Infrastructure.KeyTesting.Concrete;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence.Repositories.Concrete;
using TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.TextModels;

/// <summary>
/// Yerel LLM (OpenAI uyumlu): geçiş kimliği, kök adres, anahtarsız çağrı, sıfır maliyet, anahtar testi (<c>GET {kök}/models</c>),
/// kasada şifreli kök adres ve isteğe bağlı anahtar.
/// </summary>
public sealed class LocalOpenAiTests
{
    private readonly FakeHttpMessageHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();

    /// <summary>Katalog: <c>local-openai:&lt;model&gt;</c> geçiş kaydı model adını taşır ve ücretsizdir.</summary>
    [Fact]
    public void Catalog_resolves_passthrough_model()
    {
        var model = TestFixtures.Catalog.Find("local-openai:qwen3:8b");

        model.ShouldNotBeNull();
        model.Platform.ShouldBe(ProviderPlatforms.LocalOpenAi);
        model.ProviderModelId.ShouldBe("qwen3:8b");
        model.Key.ShouldBe("local-openai:qwen3:8b");
        model.Capability.ShouldBe(ModelCapability.Text);
        model.Pricing.InputPerMTok.ShouldBe(0m);
        model.Pricing.OutputPerMTok.ShouldBe(0m);
        TestFixtures.Catalog.Find("local-openai:").ShouldBeNull();
    }

    /// <summary>İstek kullanıcı kök adresine gider; anahtar yoksa yetki başlığı yok; maliyet 0.</summary>
    [Fact]
    public async Task Sends_to_user_base_url_without_key()
    {
        _http.EnqueueJson(OpenAiResponses.Completion("{\"title\":\"T\",\"sceneCount\":1,\"tags\":[],\"note\":null}", prompt: 5000, completion: 1000));
        var credential = new ProviderCredential(ProviderPlatforms.LocalOpenAi, string.Empty, null) { BaseUrl = "http://localhost:11434/v1" };
        var model = new LocalOpenAiTextModel(TestFixtures.Model("local-openai:mock"), credential, TestFixtures.Dependencies(_recorder), _http.CreateClient());

        var result = await model.RunAsync<SampleConcept>(TestFixtures.Request() with { Effort = EffortLevel.High }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.CostUsd.ShouldBe(0m);
        var request = _http.Requests.ShouldHaveSingleItem();
        request.Uri.ToString().ShouldBe("http://localhost:11434/v1/chat/completions");
        request.Headers.ContainsKey("Authorization").ShouldBeFalse();
        var body = request.Json;
        body["model"]!.GetValue<string>().ShouldBe("mock");
        body["response_format"]!["type"]!.GetValue<string>().ShouldBe("json_schema");
        body.ContainsKey("reasoning_effort").ShouldBeFalse();
    }

    /// <summary>Anahtar verilirse Bearer gönderilir.</summary>
    [Fact]
    public async Task Sends_bearer_when_key_given()
    {
        _http.EnqueueJson(OpenAiResponses.Completion("Merhaba"));
        var credential = new ProviderCredential(ProviderPlatforms.LocalOpenAi, "lm-studio-key", null) { BaseUrl = "http://127.0.0.1:1234/v1/" };
        var model = new LocalOpenAiTextModel(TestFixtures.Model("local-openai:llama"), credential, TestFixtures.Dependencies(_recorder), _http.CreateClient());

        var result = await model.RunTextAsync(TestFixtures.Request(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        var request = _http.Requests.ShouldHaveSingleItem();
        request.Uri.ToString().ShouldBe("http://127.0.0.1:1234/v1/chat/completions");
        request.Headers["Authorization"].ShouldBe("Bearer lm-studio-key");
    }

    /// <summary>Anahtar testi <c>GET {kök}/models</c>.</summary>
    [Fact]
    public async Task Key_test_lists_models()
    {
        _http.EnqueueJson("{\"object\":\"list\",\"data\":[{\"id\":\"mock\"}]}");
        var credential = new ProviderCredential(ProviderPlatforms.LocalOpenAi, string.Empty, null) { BaseUrl = "http://localhost:7199/v1" };

        var result = await new HttpApiKeyTester(_http.CreateClient()).TestAsync(credential, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue(result.Message);
        var request = _http.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Get);
        request.Uri.ToString().ShouldBe("http://localhost:7199/v1/models");
    }

    /// <summary>Kasa: kök adres şifreli saklanır, anahtar isteğe bağlı, boş anahtar kayıtlı anahtarı korur, kök adres zorunlu.</summary>
    [Fact]
    public async Task Vault_stores_encrypted_base_url_and_optional_key()
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = TestFixtures.Db();
        var repository = new ProviderApiKeyRepository(db);
        var protector = TestFixtures.Protector();
        var user = new TestCurrentUser();
        var vault = new RepositoryVault(repository, protector);
        var service = new ApiKeyService(user, repository, protector, vault, Substitute.For<IApiKeyTester>(), new FixedClock());

        (await service.SaveAsync(new SaveApiKeyRequest { Platform = ProviderPlatforms.LocalOpenAi }, ct)).IsFailure.ShouldBeTrue();
        var saved = await service.SaveAsync(new SaveApiKeyRequest { Platform = ProviderPlatforms.LocalOpenAi, BaseUrl = "http://localhost:7199/v1/" }, ct);

        saved.IsSuccess.ShouldBeTrue(saved.IsFailure ? saved.Error.Message : null);
        saved.Value.BaseUrl.ShouldBe("http://localhost:7199/v1");
        saved.Value.RequiresBaseUrl.ShouldBeTrue();
        saved.Value.KeyOptional.ShouldBeTrue();
        saved.Value.Last4.ShouldBeNull();
        db.ApiKeys.Single().ProtectedBaseUrl!.ShouldStartWith("v1:");
        db.ApiKeys.Single().ProtectedBaseUrl!.ShouldNotContain("localhost");

        await service.SaveAsync(new SaveApiKeyRequest { Platform = ProviderPlatforms.LocalOpenAi, ApiKey = "secret-local-9876", BaseUrl = "http://localhost:7199/v1" }, ct);
        await service.SaveAsync(new SaveApiKeyRequest { Platform = ProviderPlatforms.LocalOpenAi, BaseUrl = "http://127.0.0.1:7199/v1" }, ct);

        var credential = (await vault.GetCredentialAsync(user.GetRequiredUserId(), ProviderPlatforms.LocalOpenAi, ct)).Value;
        credential.BaseUrl.ShouldBe("http://127.0.0.1:7199/v1");
        credential.ApiKey.ShouldBe("secret-local-9876");
    }

    /// <summary>Doğrulayıcı: Yerel LLM'de anahtarsız kayıt geçerli, kök adres zorunlu ve http/https olmalı.</summary>
    [Fact]
    public void Validator_requires_base_url_but_not_key()
    {
        var validator = new SaveApiKeyRequestValidator();
        validator.Validate(new SaveApiKeyRequest { Platform = ProviderPlatforms.LocalOpenAi, BaseUrl = "http://localhost:11434/v1" }).IsValid.ShouldBeTrue();
        validator.Validate(new SaveApiKeyRequest { Platform = ProviderPlatforms.LocalOpenAi }).IsValid.ShouldBeFalse();
        validator.Validate(new SaveApiKeyRequest { Platform = ProviderPlatforms.LocalOpenAi, BaseUrl = "ftp://x/v1" }).IsValid.ShouldBeFalse();
        validator.Validate(new SaveApiKeyRequest { Platform = ProviderPlatforms.OpenAi }).IsValid.ShouldBeFalse();
        BaseUrlNormalizer.Normalize(" http://localhost:11434/v1/ ").ShouldBe("http://localhost:11434/v1");
    }
}
