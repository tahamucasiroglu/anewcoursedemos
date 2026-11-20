using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Ai;

/// <summary>
/// Ses servisi adresi boş ya da yerel değilken kaydedilen uygulama (spec 0012 C-2 — Pi varsayılanı): her çağrı
/// <see cref="AiUnavailableException"/> (<c>not-configured</c> / <c>not-local</c>).
/// </summary>
public sealed class UnavailableSpeechToText : ISpeechToText
{
    private readonly string reason;

    public UnavailableSpeechToText(string reason)
    {
        this.reason = reason;
    }

    public Task<SpeechToTextResult> TranscribeAsync(Stream audio, string contentType, CancellationToken cancellationToken) =>
        throw new AiUnavailableException(this.reason);
}
