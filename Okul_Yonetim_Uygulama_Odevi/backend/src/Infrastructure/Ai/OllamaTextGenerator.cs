using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Ai;

/// <summary>
/// Ollama metin adaptörü (spec 0012 C-1, AC-9): <c>POST api/chat</c> akışsız (<c>stream:false</c>), ayardaki model, talimat
/// sistem mesajı + içerik kullanıcı mesajı, düşük sıcaklık, en fazla token sınırı ve modelin bellekte kalma süresi
/// (<c>keep_alive</c>). Yanıttaki <c>message.content</c> döner; servis hatası, zaman aşımı ya da boş yanıt
/// <see cref="AiUnavailableException"/>. Çağrı <see cref="AiWorkGate"/> sırasından geçer. Adres ve zaman aşımı kayıtta
/// <see cref="HttpClient"/>'a verilir.
/// </summary>
public sealed class OllamaTextGenerator : IAiTextGenerator
{
    public const string KeepAlive = "10m";
    public const double Temperature = 0.3;

    private readonly HttpClient http;
    private readonly string model;
    private readonly AiWorkGate gate;

    public OllamaTextGenerator(HttpClient http, IOptions<AiOptions> options, AiWorkGate gate)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.http = http;
        this.model = options.Value.TextGenerator.Model;
        this.gate = gate;
    }

    public Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.gate.RunAsync(token => this.CallAsync(request, token), cancellationToken);
    }

    private async Task<AiTextResult> CallAsync(AiTextRequest request, CancellationToken cancellationToken)
    {
        JsonObject body = new()
        {
            ["model"] = this.model,
            ["stream"] = false,
            ["keep_alive"] = KeepAlive,
            ["messages"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = request.Instruction },
                new JsonObject { ["role"] = "user", ["content"] = request.Content }),
            ["options"] = new JsonObject { ["temperature"] = Temperature, ["num_predict"] = request.MaxTokens },
        };
        using StringContent content = new(body.ToJsonString(), Encoding.UTF8, "application/json");
        JsonElement json = await AiHttp.PostAsync(this.http, "api/chat", content, cancellationToken).ConfigureAwait(false);
        string? text = json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty("message", out JsonElement message)
            && message.ValueKind == JsonValueKind.Object
            && message.TryGetProperty("content", out JsonElement value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        return string.IsNullOrWhiteSpace(text)
            ? throw new AiUnavailableException("empty")
            : new AiTextResult(text.Trim());
    }
}
