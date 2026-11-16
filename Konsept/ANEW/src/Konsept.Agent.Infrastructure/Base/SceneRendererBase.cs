using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Serilog;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Kısıtlı sahne render'ının ortak kapısı (spec 0018 L2): iş klasöründe klasör dışını gösterebilecek bağlantı (symlink/junction) varsa render
/// hiç başlamaz (rapor T4). Motorun kendisi alt sınıftadır.
/// </summary>
/// <param name="logger">Teknik log.</param>
public abstract class SceneRendererBase(ILogger logger) : ISceneRenderer
{
    /// <summary>Gets teknik log.</summary>
    protected ILogger Logger { get; } = (logger ?? throw new ArgumentNullException(nameof(logger))).ForContext<SceneRendererBase>();

    /// <summary>İş klasöründe bağlantı (symlink/junction) var mı.</summary>
    /// <param name="directory">Klasör.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    public static bool HasReparsePoint(string directory) =>
        new DirectoryInfo(directory).EnumerateFileSystemInfos("*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0, IgnoreInaccessible = false })
            .Any(i => i.Attributes.HasFlag(FileAttributes.ReparsePoint));

    /// <inheritdoc />
    public Task<SceneRenderResult> RenderAsync(SceneRenderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (HasReparsePoint(request.JobDirectory))
        {
            Logger.Warning("İş klasöründe bağlantı (symlink/junction) var; render yapılmadı");
            return Task.FromResult(new SceneRenderResult(SceneRenderOutcome.Unsafe, "link in job folder"));
        }

        return RenderConfinedAsync(request, cancellationToken);
    }

    /// <summary>Sahneyi kısıtlı ortamda render eder (iş klasörü denetlendi).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">Durdur belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected abstract Task<SceneRenderResult> RenderConfinedAsync(SceneRenderRequest request, CancellationToken cancellationToken);
}
