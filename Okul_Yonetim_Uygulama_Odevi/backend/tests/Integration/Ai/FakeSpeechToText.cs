using OkulYonetim.Application.Ports;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Sahte ses servisi (spec 0012 — gerçek model gerekmez): verilen dökümü ve süreyi döndürür ya da verilen hatayı fırlatır;
/// gelen sesin boyutunu ve içerik türünü, çağrı sayısını saklar.
/// </summary>
public sealed class FakeSpeechToText : ISpeechToText
{
    public string Text { get; set; } = "ali bugün matematik ödevini getirmedi yarın getirsin";

    public double DurationSeconds { get; set; } = 21.4;

    public Exception? Failure { get; set; }

    public int Calls { get; private set; }

    public int LastLength { get; private set; }

    public string? LastContentType { get; private set; }

    public async Task<SpeechToTextResult> TranscribeAsync(Stream audio, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);
        this.Calls++;
        using MemoryStream copy = new();
        await audio.CopyToAsync(copy, cancellationToken);
        this.LastLength = (int)copy.Length;
        this.LastContentType = contentType;
        return this.Failure is not null ? throw this.Failure : new SpeechToTextResult(this.Text, this.DurationSeconds);
    }
}
