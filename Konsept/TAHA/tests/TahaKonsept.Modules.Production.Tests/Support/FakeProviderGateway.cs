using System.Collections.Concurrent;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Sahte LLM geçidi: her metin isteğine geçerli bir HyperFrames sahne kodu döner (isteğe bağlı ilk N yanıt geçersiz), istekleri kaydeder.
/// </summary>
public sealed class FakeProviderGateway : IProviderGateway
{
    /// <summary>Geçerli sahne kodu.</summary>
    public const string ValidCode = "<!doctype html><html lang=\"tr\"><body><div id=\"root\" data-composition-id=\"main\" data-duration=\"3\"></div>"
        + "<script>window.__timelines = window.__timelines || {}; window.__timelines[\"main\"] = gsap.timeline({ paused: true });</script></body></html>";

    /// <summary>Gelen istekler.</summary>
    public ConcurrentQueue<TextTask> Requests { get; } = new();

    /// <summary>Kaç yanıt daha geçersiz dönecek.</summary>
    public int InvalidResponsesLeft { get; set; }

    /// <summary>Çağrı başına maliyet.</summary>
    public decimal CostPerCall { get; set; } = 0.05m;

    /// <inheritdoc />
    public Task<Result<TextResult<T>>> RunStructuredAsync<T>(TextTask task, CancellationToken ct) =>
        Task.FromResult(Result.Failure<TextResult<T>>(Error.Failure("fake.unsupported", "Desteklenmez")));

    /// <inheritdoc />
    public Task<Result<TextResult<string>>> RunTextAsync(TextTask task, CancellationToken ct)
    {
        Requests.Enqueue(task);
        var text = InvalidResponsesLeft > 0 ? "geçersiz yanıt — kod yok, sadece açıklama metni burada" : "```html\n" + ValidCode + "\n```";
        if (InvalidResponsesLeft > 0)
        {
            InvalidResponsesLeft--;
        }

        var result = new TextResult<string>
        {
            Value = text,
            RawText = text,
            ModelKey = task.ModelKey,
            CostUsd = CostPerCall,
            Usage = new CallUsage { InputTokens = 1000, OutputTokens = 500 },
        };
        return Task.FromResult(Result.Success(result));
    }
}
