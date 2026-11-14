using Microsoft.Extensions.Logging.Abstractions;
using TahaKonsept.Agent.Core.VideoServer.Base;

namespace TahaKonsept.Agent.Core.Tests.Support;

/// <summary>Tek video ve sabit anahtarla çalışan test video sunucusu.</summary>
public sealed class TestVideoServer : VideoServerBase
{
    private readonly int _port;
    private readonly string _key;
    private readonly Guid _videoId;
    private readonly string _videoPath;

    /// <summary>Oluşturur.</summary>
    /// <param name="port">Port.</param>
    /// <param name="key">Akış anahtarı.</param>
    /// <param name="videoId">Video kimliği.</param>
    /// <param name="videoPath">Video dosyası.</param>
    public TestVideoServer(int port, string key, Guid videoId, string videoPath)
        : base(NullLogger.Instance)
    {
        _port = port;
        _key = key;
        _videoId = videoId;
        _videoPath = videoPath;
    }

    /// <inheritdoc />
    protected override int GetPort() => _port;

    /// <inheritdoc />
    protected override string GetStreamKey() => _key;

    /// <inheritdoc />
    protected override string? ResolveVideo(Guid videoId) => videoId == _videoId ? _videoPath : null;

    /// <inheritdoc />
    protected override string? ResolveThumbnail(Guid videoId) => null;

    /// <inheritdoc />
    protected override string? ResolvePreview(string fileName) => null;
}
