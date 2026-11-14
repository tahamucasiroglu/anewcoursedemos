using Microsoft.Extensions.Logging.Abstractions;
using TahaKonsept.Agent.Engines.Speech.Python.Base;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>Yanıtı hiç gelmeyen işçi istemcisi; protokol bozulup işçinin yeniden başlatılması istendiğinde sayar.</summary>
public sealed class HangingTtsWorkerClient : TtsWorkerClientBase
{
    private readonly BlockingTextReader _output = new();

    /// <summary>Kurar.</summary>
    public HangingTtsWorkerClient()
        : base(NullLogger.Instance)
    {
    }

    /// <summary><c>OnProtocolBrokenAsync</c> çağrı sayısı (süreçli istemcide işçinin öldürülmesi).</summary>
    public int BrokenCount { get; private set; }

    /// <summary>Gönderilen satırlar.</summary>
    public StringWriter Sent { get; } = new();

    /// <inheritdoc />
    public override bool IsRunning => true;

    /// <inheritdoc />
    protected override TextWriter Input => Sent;

    /// <inheritdoc />
    protected override TextReader Output => _output;

    /// <inheritdoc />
    protected override Task OnProtocolBrokenAsync()
    {
        BrokenCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask DisposeCoreAsync()
    {
        _output.Dispose();
        Sent.Dispose();
        return ValueTask.CompletedTask;
    }
}
