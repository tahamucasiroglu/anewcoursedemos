using System.Net;
using Konsept.Api.Tests.Fakes;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.AiGateway.Infrastructure.Catalog;
using Konsept.Modules.AiGateway.Infrastructure.Validation;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Sağlayıcı doğrulamasının HTTP eşlemesi (spec 0004 AC-10), sahte HTTP işleyicisiyle — gerçek sağlayıcı çağrısı yok: tam 1 GET,
/// yalnızca seed'deki maliyetsiz uç (token harcayan uç hiç çağrılmaz), yeniden deneme yok; 2xx kabul, 401 red, 429/5xx/zaman
/// aşımı/bağlantı hatası sonuçsuz; ucu olmayan sağlayıcıda hiç istek yok.
/// </summary>
public sealed class ProviderKeyValidatorTests
{
    /// <summary>Durum kodu → sonuç; her durumda tek GET, doğru başlık, doğru uç.</summary>
    /// <param name="status">Sahte sağlayıcının durum kodu.</param>
    /// <param name="expected">Beklenen sonuç.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData(HttpStatusCode.OK, KeyCheckOutcome.Accepted)]
    [InlineData(HttpStatusCode.Unauthorized, KeyCheckOutcome.Rejected)]
    [InlineData(HttpStatusCode.Forbidden, KeyCheckOutcome.Inconclusive)]
    [InlineData(HttpStatusCode.TooManyRequests, KeyCheckOutcome.Inconclusive)]
    [InlineData(HttpStatusCode.InternalServerError, KeyCheckOutcome.Inconclusive)]
    [InlineData(HttpStatusCode.ServiceUnavailable, KeyCheckOutcome.Inconclusive)]
    public async Task Validator_HttpOutcomes_SingleFreeGet(HttpStatusCode status, KeyCheckOutcome expected)
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new StubProviderHandler(_ => Task.FromResult(new HttpResponseMessage(status)));
        var validator = new HttpProviderKeyValidator(new StubHttpClientFactory(handler), new FixedCatalogSource(Seed(10)));

        foreach (var provider in Seed(10).Providers.Where(p => p.ValidationUrl is not null))
        {
            handler.Requests.Clear();

            (await validator.CheckAsync(provider, "test-key-123456", ct)).ShouldBe(expected, provider.Id);

            var request = handler.Requests.ShouldHaveSingleItem();
            request.Method.ShouldBe(HttpMethod.Get);
            request.RequestUri.ShouldBe(provider.ValidationUrl);
            request.RequestUri!.AbsolutePath.ShouldNotContain("messages");
            request.RequestUri.AbsolutePath.ShouldNotContain("completions");
            request.RequestUri.AbsolutePath.ShouldNotContain("images");
            if (provider.AuthStyle == ProviderAuthStyle.Anthropic)
            {
                request.Headers.GetValues("x-api-key").ShouldBe(["test-key-123456"]);
            }
            else
            {
                request.Headers.Authorization!.Parameter.ShouldBe("test-key-123456");
            }
        }
    }

    /// <summary>Seed'deki süre dolunca (sağlayıcı yanıt vermiyor) sonuçsuz; bağlantı hatası sonuçsuz; ikisinde de tek istek.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Validator_TimeoutAndConnectionError_InconclusiveSingleCall()
    {
        var ct = TestContext.Current.CancellationToken;
        var provider = Seed(0.2).Providers.First(p => p.Id == "openai");
        var hanging = new StubProviderHandler(async token =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var failing = new StubProviderHandler(_ => throw new HttpRequestException("bağlantı yok"));

        (await new HttpProviderKeyValidator(new StubHttpClientFactory(hanging), new FixedCatalogSource(Seed(0.2))).CheckAsync(provider, "test-key-123456", ct))
            .ShouldBe(KeyCheckOutcome.Inconclusive);
        (await new HttpProviderKeyValidator(new StubHttpClientFactory(failing), new FixedCatalogSource(Seed(0.2))).CheckAsync(provider, "test-key-123456", ct))
            .ShouldBe(KeyCheckOutcome.Inconclusive);
        hanging.Requests.Count.ShouldBe(1);
        failing.Requests.Count.ShouldBe(1);
    }

    /// <summary>Maliyetsiz yolu olmayan sağlayıcıda (GLM) hiç istek yapılmaz; sonuç sonuçsuz ("Doğrulanmadı"). Seed'de doğrulama süresi 10 sn.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Validator_NoFreeEndpoint_NoRequestInconclusive()
    {
        var handler = new StubProviderHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var seed = new EmbeddedCatalogSource().Catalog;
        var glm = seed.Providers.Single(p => p.Id == "glm");

        (await new HttpProviderKeyValidator(new StubHttpClientFactory(handler), new FixedCatalogSource(seed)).CheckAsync(glm, "test-key-123456", TestContext.Current.CancellationToken))
            .ShouldBe(KeyCheckOutcome.Inconclusive);
        handler.Requests.ShouldBeEmpty();
        seed.KeyValidationTimeoutSeconds.ShouldBe(10);
    }

    private static CatalogSeed Seed(double timeoutSeconds)
    {
        var embedded = new EmbeddedCatalogSource().Catalog;
        return new CatalogSeed { Version = embedded.Version, KeyValidationTimeoutSeconds = timeoutSeconds, Providers = embedded.Providers, Models = embedded.Models };
    }
}
