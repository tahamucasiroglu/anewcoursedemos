using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.VideoServer.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.VideoServer.Base;

/// <summary>
/// Kestrel tabanlı yerel dosya sunucusu: anahtar denetimi (sabit zamanlı karşılaştırma), <see cref="HttpRangeParser"/> ile
/// 200/206/416 yanıtları, <c>HEAD</c> desteği, <c>SendFileAsync</c> ile akış. Dosya çözümü (<see cref="ResolveVideo"/>,
/// <see cref="ResolveThumbnail"/>, <see cref="ResolvePreview"/>), port ve anahtar türetilen sınıftadır.
/// </summary>
public abstract class VideoServerBase : IVideoServer, IAsyncDisposable
{
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private WebApplication? _app;

    /// <summary>Oluşturur.</summary>
    /// <param name="logger">Logger.</param>
    protected VideoServerBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <inheritdoc />
    public int Port { get; private set; }

    /// <inheritdoc />
    public bool IsRunning => _app is not null;

    /// <summary>Logger.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public async Task<Result> StartAsync(CancellationToken ct)
    {
        await _lifecycle.WaitAsync(ct);
        try
        {
            if (_app is not null)
            {
                return Result.Success();
            }

            var port = GetPort();
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(VideoServerBase).Assembly.GetName().Name,
                ContentRootPath = AppContext.BaseDirectory,
            });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AddServerHeader = false;
                options.ListenAnyIP(port);
            });

            var app = builder.Build();
            MapEndpoints(app);
            try
            {
                await app.StartAsync(ct);
            }
            catch (IOException ex)
            {
                await app.DisposeAsync();
                Logger.LogError(ex, "Video sunucusu {Port} portunda başlatılamadı.", port);
                return Error.Conflict("agent.video_server_port_in_use", "Video sunucusu başlatılamadı (port " + port + " kullanımda olabilir): " + ex.Message);
            }

            _app = app;
            Port = port;
            Logger.LogInformation("Yerel video sunucusu başladı: 0.0.0.0:{Port}", port);
            return Result.Success();
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken ct)
    {
        await _lifecycle.WaitAsync(ct);
        try
        {
            var app = _app;
            _app = null;
            Port = 0;
            if (app is not null)
            {
                await app.StopAsync(ct);
                await app.DisposeAsync();
                Logger.LogInformation("Yerel video sunucusu durdu.");
            }
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _lifecycle.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Dinlenecek port.</summary>
    /// <returns>Port.</returns>
    protected abstract int GetPort();

    /// <summary>Geçerli akış anahtarı.</summary>
    /// <returns>Anahtar.</returns>
    protected abstract string GetStreamKey();

    /// <summary>Video dosyasını çözer.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Yol ya da <c>null</c>.</returns>
    protected abstract string? ResolveVideo(Guid videoId);

    /// <summary>Küçük resmi çözer.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Yol ya da <c>null</c>.</returns>
    protected abstract string? ResolveThumbnail(Guid videoId);

    /// <summary>Ses önizleme dosyasını çözer.</summary>
    /// <param name="fileName">Dosya adı.</param>
    /// <returns>Yol ya da <c>null</c>.</returns>
    protected abstract string? ResolvePreview(string fileName);

    /// <summary>Dosyayı Range desteğiyle yazar.</summary>
    /// <param name="context">HTTP bağlamı.</param>
    /// <param name="path">Dosya (yoksa 404).</param>
    /// <param name="contentType">İçerik türü.</param>
    /// <returns>Görev.</returns>
    protected static async Task ServeFileAsync(HttpContext context, string? path, string contentType)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var length = new FileInfo(path).Length;
        var response = context.Response;
        response.Headers.AcceptRanges = "bytes";
        response.Headers.CacheControl = "private, no-cache";
        response.ContentType = contentType;
        var parsed = HttpRangeParser.Parse(context.Request.Headers.Range.ToString(), length);
        long offset = 0;
        var count = length;
        switch (parsed.Outcome)
        {
            case RangeParseOutcome.Unsatisfiable:
                response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                response.Headers.ContentRange = HttpRangeParser.UnsatisfiedContentRange(length);
                return;
            case RangeParseOutcome.Partial when parsed.Range is { } range:
                response.StatusCode = StatusCodes.Status206PartialContent;
                response.Headers.ContentRange = HttpRangeParser.ContentRange(range, length);
                offset = range.Start;
                count = range.Length;
                break;
            default:
                response.StatusCode = StatusCodes.Status200OK;
                break;
        }

        response.ContentLength = count;
        if (HttpMethods.IsHead(context.Request.Method) || count == 0)
        {
            return;
        }

        try
        {
            await response.SendFileAsync(path, offset, count, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // İstemci bağlantıyı kapattı (oynatıcı atladı); normal.
        }
    }

    private void MapEndpoints(WebApplication app)
    {
        string[] methods = [HttpMethods.Get, HttpMethods.Head];
        app.MapMethods("/videos/{id:guid}", methods, (HttpContext context, Guid id) =>
            Authorized(context) ? ServeFileAsync(context, ResolveVideo(id), "video/mp4") : Forbid(context));
        app.MapMethods("/thumbs/{id:guid}", methods, (HttpContext context, Guid id) =>
            Authorized(context) ? ServeFileAsync(context, ResolveThumbnail(id), "image/jpeg") : Forbid(context));
        app.MapMethods("/previews/{name}", methods, (HttpContext context, string name) =>
            Authorized(context) ? ServeFileAsync(context, ResolvePreview(name), "audio/wav") : Forbid(context));
        app.MapGet("/health", (HttpContext context) =>
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            return context.Response.WriteAsync("ok", context.RequestAborted);
        });
    }

    private bool Authorized(HttpContext context)
    {
        var provided = context.Request.Query["k"].ToString();
        var expected = GetStreamKey();
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
    }

    private static Task Forbid(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
