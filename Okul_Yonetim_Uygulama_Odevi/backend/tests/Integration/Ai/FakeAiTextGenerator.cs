using System.Collections.Concurrent;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Sahte metin servisi (spec 0012 — gerçek model gerekmez): gelen isteği (talimat + içerik) yakalar; verilen yanıtı döndürür
/// ya da verilen hatayı fırlatır (AC-6, AC-7).
/// </summary>
public sealed class FakeAiTextGenerator : IAiTextGenerator
{
    public const string DefaultReply = "Sayın Veli, Ali bugün matematik ödevini getirmedi. Yarın getirmesini rica ederim.";

    public string Reply { get; set; } = DefaultReply;

    public Exception? Failure { get; set; }

    public ConcurrentQueue<AiTextRequest> Requests { get; } = new();

    public Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken)
    {
        this.Requests.Enqueue(request);
        return this.Failure is not null ? throw this.Failure : Task.FromResult(new AiTextResult(this.Reply));
    }
}
