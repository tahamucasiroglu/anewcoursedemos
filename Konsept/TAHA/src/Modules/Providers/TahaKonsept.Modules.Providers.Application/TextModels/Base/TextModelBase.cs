using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.AiModels.Base;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Json;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.TextModels.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.TextModels.Base;

/// <summary>
/// Metin modellerinin ortak tabanı. Somut sınıf yalnızca <see cref="SendAsync"/>'i (tek HTTP çağrısı → <see cref="ModelOutput"/>)
/// yazar; bu sınıf yeniden deneme + CallLog/maliyet (<see cref="AiModelBase"/>), <see cref="RunAsync{T}"/> için şema üretimi,
/// JSON çözme ve TEK onarım denemesi, kesilme/boş yanıt kontrollerini yapar.
/// </summary>
public abstract class TextModelBase : AiModelBase, ITextModel
{
    /// <summary>JSON onarım çağrısının sistem promptu.</summary>
    protected const string RepairSystemPrompt =
        "Sen bir JSON onarım aracısın. Verilen hatalı yanıtı, verilen JSON şemasına birebir uyan TEK bir geçerli JSON nesnesine çevir. " +
        "İçeriği koru, uydurma ekleme. Yalnızca JSON döndür; açıklama, kod çiti veya başka metin yazma.";

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    protected TextModelBase(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies)
        : base(model, credential, dependencies)
    {
    }

    /// <summary>İstekte kullanılacak sağlayıcı model kimliği.</summary>
    protected string ProviderModelId => Model.ProviderModelId;

    /// <inheritdoc />
    public async Task<Result<TextResult<string>>> RunTextAsync(TextRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var call = await CompleteAsync(request with { JsonSchema = null }, ct);
        if (call.IsFailure)
        {
            return call.Error;
        }

        var output = call.Value.Value;
        if (string.IsNullOrWhiteSpace(output.Text))
        {
            return Error.Failure(ProviderErrorCodes.EmptyResponse, $"{DisplayName} boş yanıt döndürdü.");
        }

        return ToResult(output.Text, output, call.Value, call.Value.CostUsd, call.Value.DurationMs, repaired: false, output.Usage);
    }

    /// <inheritdoc />
    public async Task<Result<TextResult<T>>> RunAsync<T>(TextRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        JsonObject schema;
        try
        {
            schema = request.JsonSchema ?? JsonSchemaBuilder.For(typeof(T));
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ProviderErrorCodes.InvalidRequest, ex.Message);
        }

        var first = await CompleteAsync(request with { JsonSchema = schema }, ct);
        if (first.IsFailure)
        {
            return first.Error;
        }

        var output = first.Value.Value;
        if (JsonResponseParser.TryParse<T>(output.Text, out var value, out var parseError))
        {
            return ToResult(value, output, first.Value, first.Value.CostUsd, first.Value.DurationMs, repaired: false, output.Usage);
        }

        if (output.Truncated)
        {
            return Error.Failure(
                ProviderErrorCodes.Truncated,
                $"{DisplayName} yanıtı çıkış token sınırına ({request.MaxOutputTokens}) takıldı; JSON tamamlanmadı. Sınırı artırın.");
        }

        Logger.LogJsonRepair(Model.Key, parseError);
        var repairRequest = new TextRequest
        {
            Context = request.Context with { Purpose = request.Context.Purpose + ":json-repair" },
            SystemPrompt = RepairSystemPrompt,
            UserPrompt = $"JSON şeması:\n{schema.ToJsonString()}\n\nHata: {parseError}\n\nHatalı yanıt:\n{output.Text}",
            MaxOutputTokens = request.MaxOutputTokens,
            Effort = Model.EffortLevels.Count > 0 ? Model.EffortLevels[0] : null,
            UseWebSearch = false,
            CacheSystemPrompt = false,
            JsonSchema = schema,
            SchemaName = request.SchemaName,
            Stream = request.Stream,
        };

        var second = await CompleteAsync(repairRequest, ct);
        if (second.IsFailure)
        {
            return second.Error;
        }

        var repaired = second.Value.Value;
        var totalUsage = output.Usage.Add(repaired.Usage);
        var totalCost = first.Value.CostUsd + second.Value.CostUsd;
        var totalDuration = first.Value.DurationMs + second.Value.DurationMs;
        if (JsonResponseParser.TryParse<T>(repaired.Text, out var fixedValue, out var secondError))
        {
            return ToResult(fixedValue, repaired, second.Value, totalCost, totalDuration, repaired: true, totalUsage);
        }

        return Error.Failure(
            ProviderErrorCodes.InvalidJson,
            $"{DisplayName} geçerli JSON döndürmedi (onarım denemesi de başarısız): {secondError}");
    }

    /// <summary>
    /// Akışlı yanıtta veri gelmeden beklenecek en uzun süre: HttpClient zaman aşımı yalnız başlıklara kadar geçerlidir; akış ortasında
    /// takılan sağlayıcı (ya da yerel sunucu) bu süre sonunda kesilir (<c>providers.timeout</c>). Her gelen satır/olayda süre sıfırlanır.
    /// </summary>
    protected virtual TimeSpan StreamIdleTimeout => TimeSpan.FromMinutes(5);

    /// <summary>Akış boşta zaman aşımı hatası.</summary>
    /// <returns>Hata.</returns>
    protected Error StreamIdleTimeoutError() =>
        ProviderHttpErrors.Timeout(PlatformDisplayName, $"akış {StreamIdleTimeout.TotalMinutes:0} dakika veri göndermedi");

    /// <summary>Tek HTTP çağrısı: isteği sağlayıcıya gönderir ve ham çıktıyı döner (yeniden deneme / kayıt tabanda).</summary>
    /// <param name="request">İstek (<see cref="TextRequest.JsonSchema"/> doluysa JSON çıktı istenir).</param>
    /// <param name="ct">İptal belirteci.</param>
    protected abstract Task<Result<ModelOutput>> SendAsync(TextRequest request, CancellationToken ct);

    /// <summary>Efor seviyesini modelin desteklediği bir seviyeye çeker (desteklemiyorsa null).</summary>
    /// <param name="requested">İstenen efor.</param>
    protected EffortLevel? ResolveEffort(EffortLevel? requested)
    {
        if (Model.EffortLevels.Count == 0)
        {
            return null;
        }

        var wanted = requested ?? Model.DefaultEffort;
        if (wanted is null)
        {
            return null;
        }

        if (Model.EffortLevels.Contains(wanted.Value))
        {
            return wanted;
        }

        var lower = Model.EffortLevels.Where(e => e <= wanted.Value).ToList();
        return lower.Count > 0 ? lower.Max() : Model.EffortLevels.Min();
    }

    /// <summary>Şemayı sisteme talimat olarak ekleyen metin (yerel JSON şema desteği olmayan sağlayıcılar için).</summary>
    /// <param name="systemPrompt">Asıl sistem promptu.</param>
    /// <param name="schema">Şema.</param>
    protected static string AppendSchemaInstruction(string systemPrompt, JsonObject schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var instruction = "Yanıtı YALNIZCA aşağıdaki JSON şemasına uyan tek bir JSON nesnesi olarak ver; açıklama veya kod çiti ekleme.\nJSON şeması:\n"
            + schema.ToJsonString();
        return string.IsNullOrWhiteSpace(systemPrompt) ? instruction : systemPrompt + "\n\n" + instruction;
    }

    /// <summary>Ölçülmüş çağrıyı yapar ve kesilme/ret durumlarını denetler.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    private async Task<Result<Metered<ModelOutput>>> CompleteAsync(TextRequest request, CancellationToken ct)
    {
        var call = await ExecuteMeteredAsync(request.Context, c => SendAsync(request, c), o => o.Usage, ct);
        if (call.IsSuccess && call.Value.Value.Truncated && request.JsonSchema is null)
        {
            Logger.LogTruncated(Model.Key, request.MaxOutputTokens);
        }

        return call;
    }

    /// <summary>Sonucu oluşturur.</summary>
    /// <typeparam name="T">Değer türü.</typeparam>
    /// <param name="value">Değer.</param>
    /// <param name="output">Son ham çıktı.</param>
    /// <param name="metered">Son ölçüm.</param>
    /// <param name="cost">Toplam maliyet.</param>
    /// <param name="duration">Toplam süre.</param>
    /// <param name="repaired">Onarıldı mı.</param>
    /// <param name="usage">Toplam kullanım.</param>
    private TextResult<T> ToResult<T>(T value, ModelOutput output, Metered<ModelOutput> metered, decimal cost, long duration, bool repaired, CallUsage usage) =>
        new()
        {
            Value = value,
            RawText = output.Text,
            ModelKey = Model.Key,
            ProviderModelId = output.ProviderModelId ?? Model.ProviderModelId,
            Usage = usage,
            CostUsd = cost,
            DurationMs = Math.Max(duration, metered.DurationMs),
            StopReason = output.StopReason,
            Repaired = repaired,
        };
}
