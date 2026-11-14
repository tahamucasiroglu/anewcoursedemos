using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Engines.Speech.Base;
using TahaKonsept.Agent.Engines.Speech.Python.Abstract;
using TahaKonsept.Agent.Engines.Speech.Python.Protocol;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Speech.Python.Base;

/// <summary>
/// JSON-lines istemcisinin ortak mantığı (taşıma bağımsız): istekleri kilitle sıraya alır, satırı <see cref="Input"/>'a yazar,
/// <see cref="Output"/>'tan protokol yanıtı gelene kadar okur (JSON olmayan satırlar loglanır), zaman aşımı ve kapanışı yönetir.
/// Somut sınıf taşımayı sağlar (ör. süreç stdin/stdout: <c>ProcessTtsWorkerClient</c>; testte bellek akışları).
/// </summary>
public abstract class TtsWorkerClientBase : ITtsWorkerClient
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    /// <summary>Logger ile kurar.</summary>
    /// <param name="logger">Logger.</param>
    protected TtsWorkerClientBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <inheritdoc />
    public abstract bool IsRunning { get; }

    /// <summary>Logger.</summary>
    protected ILogger Logger { get; }

    /// <summary>İşçinin girişi (komutlar buraya yazılır).</summary>
    protected abstract TextWriter Input { get; }

    /// <summary>İşçinin çıkışı (yanıtlar buradan okunur).</summary>
    protected abstract TextReader Output { get; }

    /// <inheritdoc />
    public async Task<Result<WorkerResponse>> SendAsync(WorkerRequest request, TimeSpan timeout, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!IsRunning)
            {
                return Result.Failure<WorkerResponse>(SpeechErrors.Worker("işçi süreci çalışmıyor."));
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);
            try
            {
                await Input.WriteLineAsync(WorkerProtocol.Serialize(request).AsMemory(), timeoutCts.Token).ConfigureAwait(false);
                await Input.FlushAsync(timeoutCts.Token).ConfigureAwait(false);
                while (true)
                {
                    var line = await Output.ReadLineAsync(timeoutCts.Token).ConfigureAwait(false);
                    if (line is null)
                    {
                        return Result.Failure<WorkerResponse>(SpeechErrors.Worker(DescribeExit()));
                    }

                    if (WorkerProtocol.TryParseResponse(line, out var response) && response is not null)
                    {
                        return Result.Success(response);
                    }

                    Logger.LogDebug("tts_worker: {Line}", line);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Yanıt yarıda kaldı: protokol senkronu bozuldu, işçi yeniden başlatılmalı.
                await OnProtocolBrokenAsync().ConfigureAwait(false);
                return Result.Failure<WorkerResponse>(SpeechErrors.Worker($"'{request.Cmd}' komutu {timeout.TotalSeconds:F0} sn içinde yanıtlanmadı."));
            }
            catch (IOException ex)
            {
                await OnProtocolBrokenAsync().ConfigureAwait(false);
                return Result.Failure<WorkerResponse>(SpeechErrors.Worker(ex.Message));
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await DisposeCoreAsync().ConfigureAwait(false);
        _gate.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Çıkış akışı kapandığında hata mesajı (ör. süreç çıkış kodu ve son stderr satırları).</summary>
    /// <returns>Açıklama.</returns>
    protected virtual string DescribeExit() => "işçi beklenmedik şekilde kapandı.";

    /// <summary>Zaman aşımı/IO hatasından sonra çağrılır; süreçli istemci süreci öldürür.</summary>
    /// <returns>Görev.</returns>
    protected virtual Task OnProtocolBrokenAsync() => Task.CompletedTask;

    /// <summary>Taşımayı kapatır.</summary>
    /// <returns>Görev.</returns>
    protected abstract ValueTask DisposeCoreAsync();
}
