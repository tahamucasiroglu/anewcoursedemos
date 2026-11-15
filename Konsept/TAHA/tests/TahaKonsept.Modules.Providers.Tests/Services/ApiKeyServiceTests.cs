using Shouldly;
using NSubstitute;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Concrete;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence.Repositories.Concrete;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.Services;

/// <summary>
/// API anahtarı kasası: şifreli saklama, son 4 hane maskesi, çözme, silme, bölge zorunluluğu, test sonucu kaydı.
/// </summary>
public sealed class ApiKeyServiceTests : IDisposable
{
    private const string PlainKey = "sk-ant-api03-SECRET-abcd";
    private readonly ProvidersDbContext _db = TestFixtures.Db();
    private readonly TestCurrentUser _user = new();
    private readonly IApiKeyTester _tester = Substitute.For<IApiKeyTester>();
    private readonly ApiKeyService _service;
    private readonly ProviderApiKeyRepository _repository;

    /// <summary>Kurucu.</summary>
    public ApiKeyServiceTests()
    {
        _repository = new ProviderApiKeyRepository(_db);
        var protector = TestFixtures.Protector();
        _service = new ApiKeyService(_user, _repository, protector, new RepositoryVault(_repository, protector), _tester, new FixedClock());
    }

    /// <summary>Anahtar şifreli saklanır, listede yalnızca son 4 hane görünür, kasa çözer.</summary>
    [Fact]
    public async Task Key_is_stored_encrypted_and_masked()
    {
        var ct = TestContext.Current.CancellationToken;
        var saved = await _service.SaveAsync(new SaveApiKeyRequest { Platform = "Anthropic", ApiKey = PlainKey }, ct);

        saved.IsSuccess.ShouldBeTrue();
        saved.Value.Platform.ShouldBe(ProviderPlatforms.Anthropic);
        saved.Value.Last4.ShouldBe("abcd");
        var entity = _db.ApiKeys.Single();
        entity.ProtectedKey.ShouldNotContain("SECRET");
        entity.ProtectedKey.ShouldStartWith("v1:");

        var list = (await _service.ListAsync(ct)).Value;
        list.Count.ShouldBe(ProviderPlatforms.Online.Count);
        var row = list.Single(k => k.Platform == ProviderPlatforms.Anthropic);
        row.Configured.ShouldBeTrue();
        row.Last4.ShouldBe("abcd");
        list.Single(k => k.Platform == ProviderPlatforms.AzureSpeech).RequiresRegion.ShouldBeTrue();
        list.Count(k => k.Configured).ShouldBe(1);

        var credential = await new RepositoryVault(_repository, TestFixtures.Protector()).GetCredentialAsync(_user.GetRequiredUserId(), "anthropic", ct);
        credential.Value.ApiKey.ShouldBe(PlainKey);
        credential.Value.ToString().ShouldNotContain("SECRET");
    }

    /// <summary>Değiştirme tek satırı günceller; silme kaldırır; olmayan anahtarı silmek NotFound.</summary>
    [Fact]
    public async Task Replace_and_delete()
    {
        var ct = TestContext.Current.CancellationToken;
        await _service.SaveAsync(new SaveApiKeyRequest { Platform = "openai", ApiKey = "sk-first-11111111" }, ct);
        await _service.SaveAsync(new SaveApiKeyRequest { Platform = "openai", ApiKey = "sk-second-22229999" }, ct);
        _db.ApiKeys.Count().ShouldBe(1);
        _db.ApiKeys.Single().Last4.ShouldBe("9999");

        (await _service.DeleteAsync("openai", ct)).IsSuccess.ShouldBeTrue();
        _db.ApiKeys.Count().ShouldBe(0);
        (await _service.DeleteAsync("openai", ct)).Error.Code.ShouldBe(ProviderErrorCodes.NotFound);
    }

    /// <summary>Azure Speech bölgesiz kaydedilemez; bilinmeyen platform reddedilir.</summary>
    [Fact]
    public async Task Region_required_and_unknown_platform_rejected()
    {
        var ct = TestContext.Current.CancellationToken;
        (await _service.SaveAsync(new SaveApiKeyRequest { Platform = "azure-speech", ApiKey = "azurekey12345" }, ct)).IsFailure.ShouldBeTrue();
        var ok = await _service.SaveAsync(new SaveApiKeyRequest { Platform = "azure-speech", ApiKey = "azurekey12345", Region = "WestEurope" }, ct);
        ok.Value.Region.ShouldBe("westeurope");
        (await _service.SaveAsync(new SaveApiKeyRequest { Platform = "nope", ApiKey = "12345678" }, ct)).Error.Code.ShouldBe(ProviderErrorCodes.UnknownPlatform);
    }

    /// <summary>Anahtar yoksa test key_missing; varsa test edici çağrılır ve sonuç kaydedilir.</summary>
    [Fact]
    public async Task Test_records_result()
    {
        var ct = TestContext.Current.CancellationToken;
        (await _service.TestAsync("fal", ct)).Error.Code.ShouldBe(ProviderErrorCodes.KeyMissing);

        await _service.SaveAsync(new SaveApiKeyRequest { Platform = "fal", ApiKey = "fal-key-12345678" }, ct);
        _tester.TestAsync(Arg.Any<ProviderCredential>(), Arg.Any<CancellationToken>())
            .Returns(new ApiKeyTestResult { Platform = "fal", Success = true, Message = "ok", StatusCode = 200 });

        var result = await _service.TestAsync("fal", ct);

        result.Value.Success.ShouldBeTrue();
        await _tester.Received(1).TestAsync(Arg.Is<ProviderCredential>(c => c.ApiKey == "fal-key-12345678" && c.Platform == "fal"), Arg.Any<CancellationToken>());
        _db.ApiKeys.Single().LastTestSucceeded.ShouldBe(true);
    }

    /// <summary>
    /// Kullanıcı ayrımı: A'nın anahtarı B'nin listesinde "yapılandırılmamış" görünür; B silemez/test edemez, kasa B için anahtar vermez;
    /// B'nin kaydı A'nın satırını değiştirmez.
    /// </summary>
    [Fact]
    public async Task Other_user_cannot_see_use_or_delete_key()
    {
        var ct = TestContext.Current.CancellationToken;
        await _service.SaveAsync(new SaveApiKeyRequest { Platform = "anthropic", ApiKey = PlainKey }, ct);
        var other = new TestCurrentUser();
        var protector = TestFixtures.Protector();
        var vault = new RepositoryVault(_repository, protector);
        var asOther = new ApiKeyService(other, _repository, protector, vault, _tester, new FixedClock());

        (await asOther.ListAsync(ct)).Value.ShouldAllBe(k => !k.Configured && k.Last4 == null);
        (await asOther.DeleteAsync("anthropic", ct)).Error.Code.ShouldBe(ProviderErrorCodes.NotFound);
        (await asOther.TestAsync("anthropic", ct)).Error.Code.ShouldBe(ProviderErrorCodes.KeyMissing);
        (await vault.GetCredentialAsync(other.GetRequiredUserId(), "anthropic", ct)).Error.Code.ShouldBe(ProviderErrorCodes.KeyMissing);
        await _tester.DidNotReceive().TestAsync(Arg.Any<ProviderCredential>(), Arg.Any<CancellationToken>());

        await asOther.SaveAsync(new SaveApiKeyRequest { Platform = "anthropic", ApiKey = "sk-other-77778888" }, ct);
        _db.ApiKeys.Count().ShouldBe(2);
        (await vault.GetCredentialAsync(_user.GetRequiredUserId(), "anthropic", ct)).Value.ApiKey.ShouldBe(PlainKey);
        (await _service.ListAsync(ct)).Value.Single(k => k.Platform == ProviderPlatforms.Anthropic).Last4.ShouldBe("abcd");
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();
}
