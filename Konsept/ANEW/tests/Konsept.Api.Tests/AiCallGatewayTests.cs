using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.AiGateway.Domain.Rules;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Çağrı kapısı (spec 0013 AC-1…AC-13, AC-17; H-1, H-3), sahte sağlayıcı ve sabit saatle — gerçek sağlayıcı çağrısı yok.
/// Reddedilen çağrıda sahte sağlayıcı hiç çağrılmaz ve kayıt oluşmaz; tekrar, zaman aşımı, kırpma ve maliyet kuralları kayıtlardan
/// ve sağlayıcıya giden çağrılardan doğrulanır.
/// </summary>
public sealed class AiCallGatewayTests
{
    private const string Sonnet = "anthropic/claude-sonnet-4-5";
    private const string Opus = "anthropic/claude-opus-4-5";
    private readonly GatewayHarness _h = new();
    private readonly Guid _user = Guid.CreateVersion7();

    /// <summary>AC-1: key'i girilmemiş ve key'i çözülemeyen sağlayıcı sağlayıcıya gitmeden Türkçe nedenle reddedilir; kayıt yok.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_NoKey_Or_UnreadableKey_RejectedBeforeProvider()
    {
        var gateway = _h.Gateway();

        var none = await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet));

        none.Error!.Message.ShouldBe("Anthropic için API key girilmemiş — Ayarlar");
        await _h.Keys.UpsertAsync(
            new UserApiKey { Id = Guid.CreateVersion7(), UserId = _user, ProviderId = "anthropic", Ciphertext = "bozuk-sifre", Hint = "sk-…abc" },
            TestContext.Current.CancellationToken);
        var unreadable = await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet));

        unreadable.Error!.Message.ShouldBe("Anthropic key'i okunamıyor — Ayarlar'dan yeniden girin");
        _h.Provider.Calls.ShouldBeEmpty();
        _h.Spend.Records.ShouldBeEmpty();
    }

    /// <summary>
    /// AC-2 (kasıtlı bozma hedefi): key kayıtlı olsa da yetkisi olmayan model sağlayıcıya gitmeden "Bu modeli kullanma yetkiniz yok"
    /// ile reddedilir; yalnızca o modele ya da tümüne yetki varsa çağrı gider.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_NoModelPermission_RejectedBeforeProvider()
    {
        _h.AddKey(_user, "anthropic");
        var gateway = _h.Gateway();

        foreach (var denied in new[] { Array.Empty<string>(), ["model.use:openai/gpt-5"], ["model.use:anthropic/*"], ["admin.panel"] })
        {
            var result = await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet, denied));
            result.Error!.Message.ShouldBe("Bu modeli kullanma yetkiniz yok");
        }

        _h.Provider.Calls.ShouldBeEmpty();
        _h.Spend.Records.ShouldBeEmpty();
        (await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet, ["model.use:" + Sonnet]))).IsSuccess.ShouldBeTrue();
        (await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet, ["model.use:*"]))).IsSuccess.ShouldBeTrue();
        _h.Provider.Calls.Count.ShouldBe(2);
    }

    /// <summary>
    /// AC-3 (kasıtlı bozma hedefi): depo başka kullanıcıların satırlarını da döndürse bile kapı yalnızca çağıranın key'ini kullanır;
    /// A'nın key'i yoksa B'nin key'i kullanılmaz, çağrı AC-1 ile reddedilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_UsesOnlyCallersOwnKey()
    {
        var other = Guid.CreateVersion7();
        var otherKey = _h.AddKey(other, "anthropic");
        var gateway = _h.Gateway(keys: new AllRowsApiKeyRepository(_h.Keys));

        (await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet))).Error!.Message.ShouldBe("Anthropic için API key girilmemiş — Ayarlar");
        _h.Provider.Calls.ShouldBeEmpty();

        var own = _h.AddKey(_user, "anthropic");
        (await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet))).IsSuccess.ShouldBeTrue();
        _h.Provider.Calls.ShouldHaveSingleItem().Call.ApiKey.ShouldBe(own);
        _h.Provider.Calls.ShouldAllBe(c => c.Call.ApiKey != otherKey);
        _h.Spend.Records.ShouldAllBe(r => r.UserId == _user);
    }

    /// <summary>
    /// AC-4 (kasıtlı bozma hedefi): sınır istenmezse ya da katalogdan yüksek istenirse katalog limiti gider; düşük istek olduğu gibi
    /// gider; katalog limiti değişince yeni limit kullanılır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_OutputLimit_ClampedToCatalog()
    {
        _h.AddKey(_user, "anthropic");
        var gateway = _h.Gateway();

        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet));
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet) with { MaxOutputTokens = 64001 });
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet) with { MaxOutputTokens = 64000 });
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet) with { MaxOutputTokens = 1000 });
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet) with { MaxOutputTokens = 1 });
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet) with { MaxOutputTokens = 0 });
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet) with { MaxOutputTokens = -5 });
        var changed = _h.Gateway(GatewayHarness.Seed(s => GatewayHarness.Model(s, Sonnet)["outputTokenLimit"] = 5000));
        await _h.CallAsync(changed, GatewayHarness.Request(_user, Sonnet));
        await _h.CallAsync(changed, GatewayHarness.Request(_user, Sonnet) with { MaxOutputTokens = 9000 });

        _h.Provider.Calls.Select(c => c.Call.MaxOutputTokens).ShouldBe([64000, 64000, 64000, 1000, 1, 64000, 64000, 5000, 5000]);
    }

    /// <summary>AC-4: sınıra takılan çıktı "yarım kaldı" olarak döner ve kaydedilir; sağlayıcı tam 1 kez çağrılır (devam/tekrar yok).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_Truncated_MarkedSingleAttempt()
    {
        _h.AddKey(_user, "anthropic");
        _h.Provider.Enqueue(ProviderAttempt.Completed("yarım metin", 100, 64000, truncated: true));

        var result = await _h.CallAsync(_h.Gateway(), GatewayHarness.Request(_user, Sonnet));

        result.Value.Outcome.ShouldBe(CallOutcome.Truncated);
        result.Value.Attempts.ShouldBe(1);
        _h.Provider.Calls.Count.ShouldBe(1);
        _h.Spend.Records.ShouldHaveSingleItem().Outcome.ShouldBe(CallOutcome.Truncated);
    }

    /// <summary>AC-5: desteklenmeyen efor sağlayıcıya gitmeden reddedilir; efor verilmezse katalog varsayılanı, verilirse o gider.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_Effort_UnsupportedRejected_DefaultApplied()
    {
        _h.AddKey(_user, "anthropic");
        var gateway = _h.Gateway();

        foreach (var (model, effort) in new[] { (Opus, "max"), (Opus, "HIGH"), (Sonnet, "low"), (Opus, "extreme") })
        {
            var rejected = await _h.CallAsync(gateway, GatewayHarness.Request(_user, model) with { Effort = effort });
            rejected.Error!.Message.ShouldBe("Bu model seçilen efor seviyesini desteklemiyor.");
        }

        _h.Provider.Calls.ShouldBeEmpty();
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Opus));
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Opus) with { Effort = "low" });
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet));

        _h.Provider.Calls.Select(c => c.Call.Effort).ShouldBe(["high", "low", null]);
        _h.Spend.Records.Select(r => r.Effort).ShouldBe(["high", "low", null]);
    }

    /// <summary>
    /// AC-6 (kasıtlı bozma hedefi): 429, 5xx ya da kurulamayan bağlantı sürerse toplam 3 deneme yapılır, 4.'sü yapılmaz; Türkçe hata
    /// döner. HTTP hata yanıtları kayıt olur (0 maliyet), kurulamayan bağlantı kayıt üretmez.
    /// </summary>
    /// <param name="status">Durum kodu; 0 = bağlantı kurulamadı.</param>
    /// <param name="message">Beklenen mesaj.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData(429, "Sağlayıcı şu an yoğun, tekrar denendi ama olmadı")]
    [InlineData(500, "Sağlayıcı şu an yoğun, tekrar denendi ama olmadı")]
    [InlineData(503, "Sağlayıcı şu an yoğun, tekrar denendi ama olmadı")]
    [InlineData(599, "Sağlayıcı şu an yoğun, tekrar denendi ama olmadı")]
    [InlineData(0, "Sağlayıcıya bağlanılamadı, tekrar denendi ama olmadı")]
    public async Task Gateway_Retryable_MaxThreeAttempts(int status, string message)
    {
        _h.AddKey(_user, "anthropic");
        _h.Provider.Default = status == 0 ? ProviderAttempt.Unreachable() : ProviderAttempt.Failed(status);

        var result = await _h.CallAsync(_h.Gateway(), GatewayHarness.Request(_user, Sonnet));

        result.Error!.Message.ShouldBe(message);
        _h.Provider.Calls.Count.ShouldBe(3);
        _h.Spend.Records.Count.ShouldBe(status == 0 ? 0 : 3);
        _h.Spend.Records.ShouldAllBe(r => r.Outcome == CallOutcome.ProviderError && r.CostUsd == 0m && !r.IsEstimated);
    }

    /// <summary>
    /// AC-6, AC-10: 429 + 429 + başarı → 2. tekrarda başarılı sonuç, sonuçlarıyla 3 kayıt; ardışık beklemeler artar (sabit saatle:
    /// en az 1 sn, sonra en az 2 sn).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_RetryDelays_Increase_SecondRetrySucceeds()
    {
        _h.AddKey(_user, "anthropic");
        _h.Provider.Enqueue(ProviderAttempt.Failed(429), ProviderAttempt.Failed(429), ProviderAttempt.Completed("tamam", 10, 5));

        var result = await _h.CallAsync(_h.Gateway(), GatewayHarness.Request(_user, Sonnet));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Attempts.ShouldBe(3);
        _h.Spend.Records.Select(r => r.Outcome).ShouldBe([CallOutcome.ProviderError, CallOutcome.ProviderError, CallOutcome.Success]);
        var at = _h.Provider.Calls.Select(c => c.At).ToList();
        var first = at[1] - at[0];
        var second = at[2] - at[1];
        first.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(1));
        second.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(2));
        second.ShouldBeGreaterThan(first);
    }

    /// <summary>AC-10: bağlantısı kurulamayan deneme kayıt üretmez; sağlayıcıya ulaşan her deneme ayrı kayıttır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_EachAttemptRecorded_ConnectionFailureNot()
    {
        _h.AddKey(_user, "anthropic");
        _h.Provider.Enqueue(ProviderAttempt.Unreachable(), ProviderAttempt.Failed(502), ProviderAttempt.Completed("tamam", 10, 5));

        var result = await _h.CallAsync(_h.Gateway(), GatewayHarness.Request(_user, Sonnet));

        result.IsSuccess.ShouldBeTrue();
        _h.Provider.Calls.Count.ShouldBe(3);
        _h.Spend.Records.Select(r => r.Outcome).ShouldBe([CallOutcome.ProviderError, CallOutcome.Success]);
    }

    /// <summary>
    /// AC-7 (kasıtlı bozma hedefi): gönderilen istek katalogdaki zaman aşımında yanıtlanmazsa tekrar edilmez (tam 1 deneme), kayıt
    /// "yanıt alınamadı" ve "tahmini" giriş-token maliyetiyle yazılır, kullanıcıya "Sağlayıcıdan yanıt alınamadı" döner; katalogdaki
    /// süre değişince yeni süre kullanılır (sürenin 1 sn öncesinde çağrı hâlâ bekler).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_Timeout_NoRetry_EstimatedRecord()
    {
        _h.AddKey(_user, "anthropic");
        _h.Provider.Hang = true;

        var result = await _h.DriveAsync(_h.Gateway().CallAsync(GatewayHarness.Request(_user, Sonnet), CancellationToken.None), TimeSpan.FromSeconds(30));

        result.Error!.Message.ShouldBe("Sağlayıcıdan yanıt alınamadı");
        _h.Provider.Calls.Count.ShouldBe(1);
        var record = _h.Spend.Records.ShouldHaveSingleItem();
        record.Outcome.ShouldBe(CallOutcome.NoResponse);
        record.IsEstimated.ShouldBeTrue();
        record.CostUsd.ShouldBeGreaterThan(0m);
        (_h.Time.GetUtcNow() - _h.Provider.Calls[0].At).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(600));

        _h.Provider.Reset();
        _h.Provider.Hang = true;
        var shorter = _h.Gateway(GatewayHarness.Seed(s => GatewayHarness.Model(s, Sonnet)["timeoutSeconds"] = 30));
        var pending = shorter.CallAsync(GatewayHarness.Request(_user, Sonnet), CancellationToken.None);
        await Eventually.WithinAsync(() => Task.FromResult(_h.Provider.Calls.Count == 1), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        _h.Time.Advance(TimeSpan.FromSeconds(29));
        await Task.Delay(50, TestContext.Current.CancellationToken);
        pending.IsCompleted.ShouldBeFalse();
        _h.Time.Advance(TimeSpan.FromSeconds(1));
        (await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Error!.Message.ShouldBe("Sağlayıcıdan yanıt alınamadı");
        _h.Provider.Calls.Count.ShouldBe(1);
    }

    /// <summary>
    /// Req 11: çağıran istek gönderildikten sonra vazgeçerse (ör. tarayıcı isteği kesildi) deneme yine "yanıt alınamadı" ve tahmini
    /// maliyetle kaydedilir; iptal yayılır, tekrar yapılmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_CallerCancelledAfterSend_StillRecorded()
    {
        _h.AddKey(_user, "anthropic");
        _h.Provider.Hang = true;
        using var cts = new CancellationTokenSource();

        var pending = _h.Gateway().CallAsync(GatewayHarness.Request(_user, Sonnet), cts.Token);
        await Eventually.WithinAsync(() => Task.FromResult(_h.Provider.Calls.Count == 1), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => pending);
        _h.Provider.Calls.Count.ShouldBe(1);
        var record = _h.Spend.Records.ShouldHaveSingleItem();
        record.Outcome.ShouldBe(CallOutcome.NoResponse);
        record.IsEstimated.ShouldBeTrue();
    }

    /// <summary>AC-8: 400, 401, 402, 403, 404 hiç tekrar edilmez (tam 1 deneme) ve Türkçe mesajla döner; kayıt 0 maliyetlidir.</summary>
    /// <param name="status">Durum kodu.</param>
    /// <param name="message">Beklenen mesaj.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData(400, "Sağlayıcı isteği kabul etmedi")]
    [InlineData(401, "Sağlayıcı API key'i reddetti")]
    [InlineData(402, "Sağlayıcı hesabında yeterli bakiye yok")]
    [InlineData(403, "Sağlayıcı bu isteğe izin vermedi")]
    [InlineData(404, "Sağlayıcı isteği kabul etmedi")]
    [InlineData(422, "Sağlayıcı isteği kabul etmedi")]
    public async Task Gateway_Non429ClientErrors_NoRetry_TurkishMessage(int status, string message)
    {
        _h.AddKey(_user, "anthropic");
        _h.Provider.Default = ProviderAttempt.Failed(status);

        var result = await _h.CallAsync(_h.Gateway(), GatewayHarness.Request(_user, Sonnet));

        result.Error!.Message.ShouldBe(message);
        _h.Provider.Calls.Count.ShouldBe(1);
        var record = _h.Spend.Records.ShouldHaveSingleItem();
        record.CostUsd.ShouldBe(0m);
        record.Outcome.ShouldBe(CallOutcome.ProviderError);
    }

    /// <summary>
    /// AC-9: başarılı metin çağrısının kaydı zaman, kullanıcı, sağlayıcı, model, efor, amaç, profil, token ve maliyeti taşır; 1 234
    /// giriş + 567 çıkış token Sonnet'te (3 / 15 USD) tam 0,012207 USD; sınır değerler (1 token, 0 token) kesirleri kaybetmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_Success_RecordWithExactCost()
    {
        _h.AddKey(_user, "anthropic");
        _h.AddKey(_user, "openai");
        var profile = Guid.CreateVersion7();
        _h.Provider.Enqueue(
            ProviderAttempt.Completed("metin", 1234, 567),
            ProviderAttempt.Completed("metin", 1, 0),
            ProviderAttempt.Completed("metin", 0, 1),
            ProviderAttempt.Completed("metin", 0, 0));
        var gateway = _h.Gateway();

        var result = await _h.CallAsync(gateway, GatewayHarness.Request(_user, Opus) with { Purpose = CallPurpose.Review, ProfileId = profile, Effort = "medium" });
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, "anthropic/claude-haiku-4-5"));
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, "openai/gpt-5-mini"));
        await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet));

        var record = _h.Spend.Records[0];
        record.UserId.ShouldBe(_user);
        record.OccurredAt.ShouldBe(_h.Provider.Calls[0].At);
        record.ProviderId.ShouldBe("anthropic");
        record.ModelId.ShouldBe(Opus);
        record.Effort.ShouldBe("medium");
        record.Purpose.ShouldBe(CallPurpose.Review);
        record.ProfileId.ShouldBe(profile);
        record.InputTokens.ShouldBe(1234);
        record.OutputTokens.ShouldBe(567);
        record.CostUsd.ShouldBe(0.020345m); // 1234 × 5 / 1M + 567 × 25 / 1M
        record.IsEstimated.ShouldBeFalse();
        result.Value.CostUsd.ShouldBe(0.020345m);
        _h.Spend.Records.Skip(1).Select(r => r.CostUsd).ShouldBe([0.000001m, 0.000002m, 0m]);
    }

    /// <summary>AC-9 (Sonnet örneği): 1 234 giriş + 567 çıkış token → 0,003702 + 0,008505 = 0,012207 USD, 6 hane hassasiyet.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_SonnetExample_SixDecimalCost()
    {
        _h.AddKey(_user, "anthropic");
        _h.Provider.Enqueue(ProviderAttempt.Completed("metin", 1234, 567));

        await _h.CallAsync(_h.Gateway(), GatewayHarness.Request(_user, Sonnet));

        decimal.Round(_h.Spend.Records.Single().CostUsd, 6).ShouldBe(0.012207m);
        _h.Spend.Records.Single().CostUsd.ShouldBe(0.012207m);
    }

    /// <summary>AC-9: yanıtında maliyet bildiren sağlayıcıda (OpenRouter) kayıt bildirilen maliyeti taşır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_OpenRouter_ReportedCost()
    {
        _h.AddKey(_user, "openrouter");
        _h.Provider.Enqueue(ProviderAttempt.Completed("metin", 1000, 1000) with { ReportedCostUsd = 0.0123456789m });

        await _h.CallAsync(_h.Gateway(), GatewayHarness.Request(_user, "openrouter/anthropic/claude-sonnet-4.5"));

        var record = _h.Spend.Records.ShouldHaveSingleItem();
        record.CostUsd.ShouldBe(0.0123456789m);
        record.IsEstimated.ShouldBeFalse();
    }

    /// <summary>AC-10: görsel çağrısının maliyeti görsel adedi × görsel fiyatıdır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_Image_CostPerImage()
    {
        _h.AddKey(_user, "openai");
        _h.Provider.Enqueue(ProviderAttempt.Completed(null, null, null) with { Images = ["aGVsbG8=", "d29ybGQ="] });

        var result = await _h.CallAsync(_h.Gateway(), GatewayHarness.Request(_user, "openai/gpt-image-1") with { Purpose = CallPurpose.Image, ImageCount = 2 });

        result.Value.Images.Count.ShouldBe(2);
        _h.Provider.Calls.Single().Call.ImageCount.ShouldBe(2);
        _h.Provider.Calls.Single().Call.MaxOutputTokens.ShouldBeNull();
        var record = _h.Spend.Records.ShouldHaveSingleItem();
        record.ImageCount.ShouldBe(2);
        record.CostUsd.ShouldBe(0.084m);
        record.IsEstimated.ShouldBeFalse();
    }

    /// <summary>
    /// AC-11: token sayısı bildirilmeyen başarılı ve yarım yanıtın kaydı giriş-token tahminiyle (0'dan büyük, "tahmini"); hata yanıtının
    /// kaydı 0 maliyetle.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_UnknownTokens_ErrorZero_SuccessEstimated()
    {
        _h.AddKey(_user, "anthropic");
        _h.Provider.Enqueue(
            ProviderAttempt.Completed("metin", null, null),
            ProviderAttempt.Completed("metin", null, null, truncated: true),
            ProviderAttempt.Failed(400));
        var gateway = _h.Gateway();
        var request = GatewayHarness.Request(_user, Sonnet);

        await _h.CallAsync(gateway, request);
        await _h.CallAsync(gateway, request);
        await _h.CallAsync(gateway, request);

        var expected = (request.Prompt.Length + 3) / 4 * 3m / 1_000_000m;
        var records = _h.Spend.Records;
        records[0].IsEstimated.ShouldBeTrue();
        records[0].CostUsd.ShouldBe(expected);
        records[0].CostUsd.ShouldBeGreaterThan(0m);
        records[1].Outcome.ShouldBe(CallOutcome.Truncated);
        records[1].IsEstimated.ShouldBeTrue();
        records[1].CostUsd.ShouldBe(expected);
        records[2].CostUsd.ShouldBe(0m);
        records[2].IsEstimated.ShouldBeFalse();
    }

    /// <summary>AC-12: katalog fiyatı değişince eski kayıtların maliyeti değişmez; yeni çağrılar yeni fiyatla kaydedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_PriceChange_OldRecordsUnchanged()
    {
        _h.AddKey(_user, "anthropic");
        _h.Provider.Default = ProviderAttempt.Completed("metin", 1_000_000, 0);

        await _h.CallAsync(_h.Gateway(), GatewayHarness.Request(_user, Sonnet));
        var repriced = _h.Gateway(GatewayHarness.Seed(s => GatewayHarness.Model(s, Sonnet)["inputPricePerMillionUsd"] = 7.123456m));
        await _h.CallAsync(repriced, GatewayHarness.Request(_user, Sonnet));

        _h.Spend.Records.Select(r => r.CostUsd).ShouldBe([3.00m, 7.123456m]);
    }

    /// <summary>
    /// AC-13: çağrı sürerken key silinirse o çağrı tamamlanır ve kaydı yazılır; sonraki çağrı key yok diye reddedilir; geçmiş kayıt
    /// yerinde kalır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_KeyDeletedDuringCall_Completes_NextRejected_HistoryStays()
    {
        var ct = TestContext.Current.CancellationToken;
        _h.AddKey(_user, "anthropic");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _h.Provider.Gate = release.Task;
        var gateway = _h.Gateway();

        var inFlight = gateway.CallAsync(GatewayHarness.Request(_user, Sonnet), CancellationToken.None);
        await Eventually.WithinAsync(() => Task.FromResult(_h.Provider.Calls.Count == 1), TimeSpan.FromSeconds(5), ct);
        (await _h.Keys.DeleteAsync(_user, "anthropic", ct)).ShouldBeTrue();
        release.SetResult();

        (await inFlight.WaitAsync(TimeSpan.FromSeconds(5), ct)).IsSuccess.ShouldBeTrue();
        _h.Provider.Gate = null;
        (await _h.CallAsync(gateway, GatewayHarness.Request(_user, Sonnet))).Error!.Message.ShouldBe("Anthropic için API key girilmemiş — Ayarlar");
        _h.Provider.Calls.Count.ShouldBe(1);
        _h.Spend.Records.ShouldHaveSingleItem().ProviderId.ShouldBe("anthropic");
        var (start, end) = IstanbulMonth.Of(_h.Time.GetUtcNow()); // review r1 L-4: silinen key'in kaydı aylık toplama girmeye devam eder.
        (await _h.Spend.SumAsync(_user, start, end, ct)).ShouldBe(_h.Spend.Records.Single().CostUsd);
        _h.Spend.Records.Single().CostUsd.ShouldBeGreaterThan(0m);
    }

    /// <summary>AC-17: ay toplamı ne kadar yüksek olursa olsun çağrı engellenmez (harcama yalnızca bilgi).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_HighSpend_NeverBlocks()
    {
        _h.AddKey(_user, "anthropic");
        await _h.Spend.AddAsync(
            new SpendRecord { Id = Guid.CreateVersion7(), UserId = _user, OccurredAt = _h.Time.GetUtcNow(), ProviderId = "anthropic", ModelId = Sonnet, CostUsd = 99_999_999m },
            TestContext.Current.CancellationToken);

        var result = await _h.CallAsync(_h.Gateway(), GatewayHarness.Request(_user, Sonnet));

        result.IsSuccess.ShouldBeTrue();
        _h.Provider.Calls.Count.ShouldBe(1);
    }
}
