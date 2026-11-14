using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.TextModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;

/// <summary>
/// Anthropic Claude metin modeli (resmi <c>Anthropic</c> C# SDK, Messages API). Sistem promptu <c>cache_control</c> ile önbelleğe
/// alınır, efor <c>output_config.effort</c>, yapılandırılmış çıktı <c>output_config.format</c> (json_schema), web araması
/// <c>web_search_20260209</c> sunucu aracı, uzun çıktılarda (ör. video kodu, 64000 token) akış. SDK yeniden denemesi kapalı
/// (tabandaki yeniden deneme + CallLog kullanılır).
/// </summary>
public sealed class AnthropicTextModel : TextModelBase
{
    /// <summary>Bir istekte en fazla web araması.</summary>
    public const int MaxWebSearches = 5;

    private readonly AnthropicClient _client;

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi (SDK'ya verilir).</param>
    public AnthropicTextModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
        : base(model, credential, dependencies)
    {
        ArgumentNullException.ThrowIfNull(http);
        _client = new AnthropicClient
        {
            ApiKey = credential.ApiKey,
            HttpClient = http,
            MaxRetries = 0,
            Timeout = TimeSpan.FromMinutes(15),
        };
    }

    /// <inheritdoc />
    protected override async Task<Result<ModelOutput>> SendAsync(TextRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parameters = BuildParameters(request);
        try
        {
            var output = request.Stream
                ? await StreamAsync(parameters, ct)
                : FromMessage(await _client.Messages.Create(parameters, ct));
            return output.StopReason == "refusal"
                ? ProviderBilledUsage.Attach(Error.Failure(ProviderErrorCodes.Refused, $"{DisplayName} isteği güvenlik nedeniyle yanıtlamadı."), output.Usage)
                : output;
        }
        catch (AnthropicApiException ex)
        {
            return ProviderHttpErrors.FromStatus(PlatformDisplayName, (int)ex.StatusCode, ex.Message);
        }
        catch (OperationCanceledException) when (request.Stream && !ct.IsCancellationRequested)
        {
            return StreamIdleTimeoutError();
        }
    }

    /// <summary>SDK parametrelerini kurar.</summary>
    /// <param name="request">İstek.</param>
    private MessageCreateParams BuildParameters(TextRequest request)
    {
        // Web araması atıf (citation) üretir; atıflar output_config.format ile birlikte kullanılamadığından bu durumda
        // şema sistem talimatı olarak verilir ve JSON tabandaki ayrıştırma/onarımla alınır.
        var nativeSchema = request.JsonSchema is not null && !request.UseWebSearch;
        var system = request.JsonSchema is not null && !nativeSchema
            ? AppendSchemaInstruction(request.SystemPrompt, request.JsonSchema)
            : request.SystemPrompt;

        var effort = ResolveEffort(request.Effort);
        OutputConfig? outputConfig = (effort, nativeSchema) switch
        {
            ({ } e, true) => new OutputConfig { Effort = MapEffort(e), Format = new JsonOutputFormat { Schema = ToSchemaDictionary(request) } },
            ({ } e, false) => new OutputConfig { Effort = MapEffort(e) },
            (null, true) => new OutputConfig { Format = new JsonOutputFormat { Schema = ToSchemaDictionary(request) } },
            _ => null,
        };

        // SDK, atanan null değerleri JSON'a "null" olarak yazar; bu yüzden opsiyonel alanlar yalnızca doluyken atanır.
        var parameters = new MessageCreateParams
        {
            Model = ProviderModelId,
            MaxTokens = request.MaxOutputTokens,
            Messages = [new UserMessageParam { Content = request.UserPrompt }],
        };
        if (!string.IsNullOrWhiteSpace(system))
        {
            var block = request.CacheSystemPrompt
                ? new TextBlockParam { Text = system, CacheControl = new CacheControlEphemeral() }
                : new TextBlockParam { Text = system };
            parameters = parameters with { System = new List<TextBlockParam> { block } };
        }

        if (outputConfig is not null)
        {
            parameters = parameters with { OutputConfig = outputConfig };
        }

        if (request.UseWebSearch)
        {
            parameters = parameters with { Tools = [new ToolUnion(new WebSearchTool20260209 { MaxUses = MaxWebSearches })] };
        }

        return parameters;
    }

    /// <summary>Akışla çağırır ve olayları birleştirir.</summary>
    /// <param name="parameters">Parametreler.</param>
    /// <param name="ct">İptal belirteci.</param>
    private async Task<ModelOutput> StreamAsync(MessageCreateParams parameters, CancellationToken ct)
    {
        var text = new StringBuilder();
        var usage = CallUsage.Empty;
        string? stopReason = null;
        string? modelId = null;
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(ct);
        idle.CancelAfter(StreamIdleTimeout);
        await foreach (var streamEvent in _client.Messages.CreateStreaming(parameters, idle.Token))
        {
            idle.CancelAfter(StreamIdleTimeout);
            if (streamEvent.TryPickStart(out var start))
            {
                modelId = start.Message.Model.ToString();
                usage = ToUsage(start.Message.Usage);
            }
            else if (streamEvent.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var textDelta))
            {
                text.Append(textDelta.Text);
            }
            else if (streamEvent.TryPickDelta(out var messageDelta))
            {
                stopReason = messageDelta.Delta.StopReason?.ToString() ?? stopReason;
                var u = messageDelta.Usage;
                usage = usage with
                {
                    OutputTokens = u.OutputTokens,
                    InputTokens = u.InputTokens ?? usage.InputTokens,
                    CacheReadTokens = u.CacheReadInputTokens ?? usage.CacheReadTokens,
                    CacheWriteTokens = u.CacheCreationInputTokens ?? usage.CacheWriteTokens,
                    WebSearchRequests = (int?)u.ServerToolUse?.WebSearchRequests ?? usage.WebSearchRequests,
                };
            }
        }

        return new ModelOutput(text.ToString(), usage, stopReason, stopReason == "max_tokens", modelId);
    }

    /// <summary>Akışsız yanıtı çevirir.</summary>
    /// <param name="message">SDK mesajı.</param>
    private static ModelOutput FromMessage(Message message)
    {
        var text = new StringBuilder();
        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                text.Append(textBlock.Text);
            }
        }

        var stopReason = message.StopReason?.ToString();
        return new ModelOutput(text.ToString(), ToUsage(message.Usage), stopReason, stopReason == "max_tokens", message.Model.ToString());
    }

    /// <summary>SDK kullanımını çevirir.</summary>
    /// <param name="usage">SDK kullanımı.</param>
    private static CallUsage ToUsage(Usage usage) => new()
    {
        InputTokens = usage.InputTokens,
        OutputTokens = usage.OutputTokens,
        CacheReadTokens = usage.CacheReadInputTokens ?? 0,
        CacheWriteTokens = usage.CacheCreationInputTokens ?? 0,
        WebSearchRequests = (int)(usage.ServerToolUse?.WebSearchRequests ?? 0),
    };

    /// <summary>Efor seviyesini SDK değerine çevirir.</summary>
    /// <param name="effort">Efor.</param>
    private static Effort MapEffort(EffortLevel effort) => effort switch
    {
        EffortLevel.Low => Effort.Low,
        EffortLevel.Medium => Effort.Medium,
        EffortLevel.High => Effort.High,
        EffortLevel.XHigh => Effort.Xhigh,
        _ => Effort.Max,
    };

    /// <summary>Şemayı SDK'nın beklediği sözlüğe çevirir.</summary>
    /// <param name="request">İstek (şema dolu).</param>
    private static Dictionary<string, JsonElement> ToSchemaDictionary(TextRequest request)
    {
        using var document = JsonDocument.Parse(request.JsonSchema!.ToJsonString());
        return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
    }
}
