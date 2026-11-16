using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Konsept.Agent.Infrastructure.Base;
using Serilog;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Yerel video akış sunucusu (spec 0017 Req 21, Ç-1): yalnızca <c>127.0.0.1</c>'e bağlanır (başka bilgisayardan erişilemez), uzak adres
/// loopback değilse reddeder. <c>GET/HEAD /video/&lt;id&gt;?t=&lt;yetki&gt;</c> → dosya (Range destekli, 206); yetkisiz/süresi dolmuş/başka
/// videonun yetkisi → 403. Tarayıcının yerel ağ ön sorgusuna (<c>OPTIONS</c>) izin başlığıyla cevap verir. İstek yolu ve yetki hiçbir zaman
/// loglanmaz (0008 Req 8, AC-28). Her cevaptan sonra bağlantı kapanır.
/// </summary>
/// <param name="videoFolder">Video klasörünü veren kaynak.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Teknik log.</param>
/// <param name="preferredPort">Tercih edilen port (doluysa sistem seçer; 0 = sistem).</param>
public sealed class LocalVideoServer(Func<string> videoFolder, TimeProvider time, ILogger logger, int preferredPort = LocalVideoServer.DefaultPort)
    : VideoStreamHostBase(videoFolder, time)
{
    /// <summary>Varsayılan port.</summary>
    public const int DefaultPort = 47615;

    private const int MaxHeaderBytes = 8192;

    private static readonly string[] CommonHeaders = ["Access-Control-Allow-Private-Network: true", "Connection: close"];

    private readonly ILogger _logger = (logger ?? Serilog.Core.Logger.None).ForContext<LocalVideoServer>();
    private readonly CancellationTokenSource _stop = new();
    private TcpListener? _listener;
    private Task _accept = Task.CompletedTask;

    /// <inheritdoc />
    public override int Port => _listener?.LocalEndpoint is IPEndPoint endpoint ? endpoint.Port : 0;

    /// <summary>Gets dinlenen adres (yalnızca loopback).</summary>
    public IPAddress? Address => (_listener?.LocalEndpoint as IPEndPoint)?.Address;

    /// <inheritdoc />
    public override void Start()
    {
        if (_listener is not null)
        {
            return;
        }

        try
        {
            _listener = new TcpListener(IPAddress.Loopback, preferredPort);
            _listener.Start();
        }
        catch (SocketException)
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
        }

        _accept = AcceptLoopAsync(_stop.Token);
        _logger.Information("Yerel video akışı dinleniyor: port {Port}", Port);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync().ConfigureAwait(false);
        _listener?.Stop();
        try
        {
            await _accept.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Kapanış.
        }

        _stop.Dispose();
    }

    private static async Task WriteHeadAsync(Stream stream, int status, string reason, IReadOnlyList<string> headers, CancellationToken cancellationToken)
    {
        var text = new StringBuilder("HTTP/1.1 ").Append(status.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(reason).Append("\r\n");
        foreach (var header in headers.Concat(CommonHeaders))
        {
            text.Append(header).Append("\r\n");
        }

        await stream.WriteAsync(Encoding.ASCII.GetBytes(text.Append("\r\n").ToString()), cancellationToken).ConfigureAwait(false);
    }

    private static (long Start, long End)? ParseRange(string? range, long length)
    {
        if (range is null || !range.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase) || length == 0)
        {
            return null;
        }

        var parts = range[6..].Split(',')[0].Split('-');
        if (parts.Length != 2)
        {
            return null;
        }

        long start, end;
        if (parts[0].Length == 0)
        {
            if (!long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var suffix) || suffix <= 0)
            {
                return null;
            }

            (start, end) = (Math.Max(0, length - suffix), length - 1);
        }
        else
        {
            if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out start))
            {
                return null;
            }

            end = parts[1].Length == 0 || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var e) ? length - 1 : Math.Min(e, length - 1);
        }

        return start <= end && start < length ? (start, end) : null;
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener!.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (SocketException)
            {
                continue;
            }

            _ = Task.Run(() => ServeAsync(client, cancellationToken), CancellationToken.None);
        }
    }

    private async Task ServeAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                if (client.Client.RemoteEndPoint is not IPEndPoint { } remote || !IPAddress.IsLoopback(remote.Address))
                {
                    _logger.Warning("Yerel video akışı: başka bilgisayardan gelen istek reddedildi");
                    await WriteHeadAsync(stream, 403, "Forbidden", ["Content-Length: 0"], cancellationToken).ConfigureAwait(false);
                    return;
                }

                var head = await ReadHeadAsync(stream, cancellationToken).ConfigureAwait(false);
                if (head is null)
                {
                    return;
                }

                var lines = head.Split("\r\n");
                var request = lines[0].Split(' ');
                var headers = lines.Skip(1).Select(l => l.Split(':', 2)).Where(p => p.Length == 2)
                    .GroupBy(p => p[0].Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First()[1].Trim(), StringComparer.OrdinalIgnoreCase);
                if (request.Length < 2)
                {
                    return;
                }

                if (request[0] == "OPTIONS")
                {
                    await WriteHeadAsync(stream, 204, "No Content", ["Access-Control-Allow-Origin: *", "Access-Control-Allow-Methods: GET, HEAD", "Access-Control-Allow-Headers: Range", "Content-Length: 0"], cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                var path = Resolve(request[1]);
                if (request[0] is not ("GET" or "HEAD") || path is null)
                {
                    _logger.Warning("Yerel video akışı: yetkisiz istek reddedildi");
                    await WriteHeadAsync(stream, 403, "Forbidden", ["Content-Length: 0"], cancellationToken).ConfigureAwait(false);
                    return;
                }

                await ServeFileAsync(stream, path, request[0] == "HEAD", headers.GetValueOrDefault("Range"), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
            {
                // Oynatıcı bağlantıyı kapattı (atlama, durdurma); olağan.
            }
        }
    }

    private string? Resolve(string target)
    {
        var query = target.IndexOf('?', StringComparison.Ordinal);
        var route = query < 0 ? target : target[..query];
        if (!route.StartsWith("/video/", StringComparison.Ordinal) || !Guid.TryParseExact(route[7..], "N", out var videoId))
        {
            return null;
        }

        var token = query < 0
            ? null
            : target[(query + 1)..].Split('&').Select(p => p.Split('=', 2)).FirstOrDefault(p => p.Length == 2 && p[0] == "t")?[1];
        return Authorize(videoId, token);
    }

    private async Task ServeFileAsync(Stream stream, string path, bool headOnly, string? range, CancellationToken cancellationToken)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var length = file.Length;
        var span = ParseRange(range, length);
        var (start, end) = span ?? (0, length - 1);
        var count = length == 0 ? 0 : end - start + 1;
        var headers = new List<string>
        {
            "Content-Type: video/mp4",
            "Accept-Ranges: bytes",
            "Content-Length: " + count.ToString(CultureInfo.InvariantCulture),
            "Cache-Control: no-store",
        };
        if (span is not null)
        {
            headers.Add(string.Format(CultureInfo.InvariantCulture, "Content-Range: bytes {0}-{1}/{2}", start, end, length));
        }

        await WriteHeadAsync(stream, span is null ? 200 : 206, span is null ? "OK" : "Partial Content", headers, cancellationToken).ConfigureAwait(false);
        if (headOnly || count == 0)
        {
            return;
        }

        file.Seek(start, SeekOrigin.Begin);
        var buffer = new byte[81920];
        var left = count;
        while (left > 0)
        {
            var read = await file.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, left)), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            left -= read;
        }
    }

    private async Task<string?> ReadHeadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[MaxHeaderBytes];
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return null;
            }

            total += read;
            var text = Encoding.ASCII.GetString(buffer, 0, total);
            var end = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (end >= 0)
            {
                return text[..end];
            }
        }

        _logger.Warning("Yerel video akışı: başlık çok uzun, istek reddedildi");
        return null;
    }
}
