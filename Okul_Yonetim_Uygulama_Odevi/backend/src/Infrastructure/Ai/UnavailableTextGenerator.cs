using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Ai;

/// <summary>
/// Metin servisi adresi boş ya da yerel değilken kaydedilen uygulama (spec 0012 C-2 — Pi varsayılanı): her çağrı
/// <see cref="AiUnavailableException"/> (<c>not-configured</c> / <c>not-local</c>); mesajlaşma etkilenmez.
/// </summary>
public sealed class UnavailableTextGenerator : IAiTextGenerator
{
    private readonly string reason;

    public UnavailableTextGenerator(string reason)
    {
        this.reason = reason;
    }

    public Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken) =>
        throw new AiUnavailableException(this.reason);
}
