using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using Konsept.Agent.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Yerel akış yetkilerinin ortak mantığı (spec 0017 Req 21, AC-25): yetki rastgele 256 bit, yalnızca bir videoya bağlı; ilk açılış
/// <see cref="OpenWindow"/> içinde olmalı, açıldıktan sonra istekler sürdükçe <see cref="IdleWindow"/> boyunca geçerli kalır (yetki süresinden
/// uzun video kesilmez). Yalnızca video klasöründeki dosya adı kabul edilir (yol yok). Dinleme ve HTTP alt sınıftadır.
/// </summary>
/// <param name="videoFolder">Video klasörünü veren kaynak.</param>
/// <param name="time">Saat.</param>
public abstract class VideoStreamHostBase(Func<string> videoFolder, TimeProvider time) : IVideoStreamHost
{
    /// <summary>İlk açılış için yetkinin ömrü.</summary>
    public static readonly TimeSpan OpenWindow = TimeSpan.FromSeconds(60);

    /// <summary>Açılmış akışın boşta kalabileceği süre.</summary>
    public static readonly TimeSpan IdleWindow = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, StreamGrant> _grants = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public abstract int Port { get; }

    /// <inheritdoc />
    public abstract void Start();

    /// <inheritdoc />
    public AgentStreamReply Open(AgentStreamRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = request.FileName;
        if (string.IsNullOrWhiteSpace(name) || !string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal))
        {
            return new AgentStreamReply(null, AgentStreamReply.NotFound);
        }

        var path = Path.Combine(videoFolder(), name);
        if (!File.Exists(path))
        {
            return new AgentStreamReply(null, AgentStreamReply.NotFound);
        }

        var now = time.GetUtcNow();
        foreach (var (key, old) in _grants)
        {
            if (old.LastUse is { } last ? now - last > IdleWindow : now > old.OpenUntil)
            {
                _grants.TryRemove(key, out _); // Süresi dolan yetkiler birikmez (review r1 L-3).
            }
        }

        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        _grants[token] = new StreamGrant(request.VideoId, path, time.GetUtcNow() + OpenWindow);
        return new AgentStreamReply(
            "http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture) + "/video/" + request.VideoId.ToString("N", CultureInfo.InvariantCulture) + "?t=" + token,
            null);
    }

    /// <inheritdoc />
    public abstract ValueTask DisposeAsync();

    /// <summary>İsteği yetkilendirir: yetki var, aynı video, ilk açılış süresinde ya da açık akış boşta kalmamış.</summary>
    /// <param name="videoId">İstenen video (yoldan).</param>
    /// <param name="token">Yetki (sorgudan).</param>
    /// <returns>Dosya yolu ya da <c>null</c> (red).</returns>
    protected string? Authorize(Guid videoId, string? token)
    {
        if (token is null || !_grants.TryGetValue(token, out var grant) || grant.VideoId != videoId)
        {
            return null;
        }

        var now = time.GetUtcNow();
        var valid = grant.LastUse is { } last ? now - last <= IdleWindow : now <= grant.OpenUntil;
        if (!valid)
        {
            _grants.TryRemove(token, out _);
            return null;
        }

        _grants[token] = grant with { LastUse = now };
        return grant.Path;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
