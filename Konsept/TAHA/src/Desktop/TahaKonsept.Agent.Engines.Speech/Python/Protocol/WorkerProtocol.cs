using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TahaKonsept.Agent.Engines.Speech.Python.Protocol;

/// <summary>
/// JSON-lines protokolü (durumsuz): istek fabrikaları (<see cref="Ping"/>, <see cref="Load"/>, <see cref="Synth"/>, <see cref="Quit"/>),
/// tek satıra serileştirme (<see cref="Serialize"/>) ve yanıt satırı ayrıştırma (<see cref="TryParseResponse"/>; JSON olmayan
/// ya da <c>ok</c> alanı olmayan satırlar log satırı sayılır).
/// </summary>
public static class WorkerProtocol
{
    /// <summary>Ping komutu adı.</summary>
    public const string PingCommand = "ping";

    /// <summary>Load komutu adı.</summary>
    public const string LoadCommand = "load";

    /// <summary>Synth komutu adı.</summary>
    public const string SynthCommand = "synth";

    /// <summary>Quit komutu adı.</summary>
    public const string QuitCommand = "quit";

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };

    /// <summary>Ping isteği.</summary>
    /// <returns>İstek.</returns>
    public static WorkerRequest Ping() => new() { Cmd = PingCommand };

    /// <summary>Quit isteği.</summary>
    /// <returns>İstek.</returns>
    public static WorkerRequest Quit() => new() { Cmd = QuitCommand };

    /// <summary>Model yükleme isteği.</summary>
    /// <param name="engine">Adapter modülü (ör. <c>chatterbox</c>).</param>
    /// <param name="model">Model kimliği (isteğe bağlı).</param>
    /// <param name="device"><c>cuda</c> / <c>cpu</c>.</param>
    /// <param name="options">Ek seçenekler.</param>
    /// <returns>İstek.</returns>
    public static WorkerRequest Load(string engine, string? model, string device, IReadOnlyDictionary<string, object?>? options = null) =>
        new() { Cmd = LoadCommand, Engine = engine, Model = model, Device = device, Options = options is { Count: > 0 } ? options : null };

    /// <summary>Sentez isteği.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="language">Dil.</param>
    /// <param name="voice">Ses.</param>
    /// <param name="referenceAudio">Referans ses.</param>
    /// <param name="speed">Hız.</param>
    /// <param name="output">Çıktı yolu.</param>
    /// <returns>İstek.</returns>
    public static WorkerRequest Synth(string text, string language, string? voice, string? referenceAudio, double speed, string output) =>
        new()
        {
            Cmd = SynthCommand,
            Text = text,
            Lang = language,
            Voice = string.IsNullOrWhiteSpace(voice) ? null : voice,
            RefAudio = string.IsNullOrWhiteSpace(referenceAudio) ? null : referenceAudio,
            Speed = speed,
            Out = output,
        };

    /// <summary>İsteği tek satır JSON'a çevirir (satır sonu yok; Türkçe karakterler kaçışsız).</summary>
    /// <param name="request">İstek.</param>
    /// <returns>JSON satırı.</returns>
    public static string Serialize(WorkerRequest request) => JsonSerializer.Serialize(request, Options);

    /// <summary>Bir stdout satırını yanıt olarak ayrıştırmayı dener.</summary>
    /// <param name="line">Satır.</param>
    /// <param name="response">Başarılıysa yanıt.</param>
    /// <returns>Satır geçerli bir protokol yanıtıysa <c>true</c>.</returns>
    public static bool TryParseResponse(string? line, out WorkerResponse? response)
    {
        response = null;
        var trimmed = line?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed[0] != '{')
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("ok", out var ok) ||
                ok.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return false;
            }

            response = document.RootElement.Deserialize<WorkerResponse>(Options);
            return response is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
