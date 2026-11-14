using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Tools.Base;

/// <summary>
/// <see cref="IToolProvisioner"/> tabanı: araç <see cref="IToolLocator"/> ile bulunursa hemen döner; bulunamazsa
/// <see cref="InstallCoreAsync"/> ile kurar ve yeniden arar (kurulum "başarılı" deyip araç yine yoksa anlaşılır hata).
/// Aynı anda iki aşama aynı aracı istemesin diye kurulum tek kapıdan (semafor) geçer.
/// </summary>
public abstract class ToolProvisionerBase : IToolProvisioner, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    /// <summary>Oluşturur.</summary>
    /// <param name="tools">Araç bulucu.</param>
    protected ToolProvisionerBase(IToolLocator tools)
    {
        Tools = tools;
    }

    /// <summary>Araç bulucu.</summary>
    protected IToolLocator Tools { get; }

    /// <inheritdoc />
    public async Task<Result<string>> EnsureAsync(ToolKind kind, IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        var existing = Tools.Find(kind);
        if (existing is not null)
        {
            return existing;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            existing = Tools.Find(kind);
            if (existing is not null)
            {
                return existing;
            }

            var installed = await InstallCoreAsync(kind, progress, ct).ConfigureAwait(false);
            return installed.IsFailure ? Result.Failure<string>(installed.Error) : Tools.Require(kind);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Kaynakları bırakır.</summary>
    /// <param name="disposing">Yönetilen kaynaklar da bırakılsın mı.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _gate.Dispose();
        }

        _disposed = true;
    }

    /// <summary>Eksik aracı kurar.</summary>
    /// <param name="kind">Araç.</param>
    /// <param name="progress">İlerleme.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da hata.</returns>
    protected abstract Task<Result> InstallCoreAsync(ToolKind kind, IProgress<EngineSetupProgress>? progress, CancellationToken ct);
}
