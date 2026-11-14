using Microsoft.Extensions.Logging.Abstractions;
using TahaKonsept.Agent.Engines.Speech.Python.Base;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>Önceden yazılmış çıktı satırlarını okuyan, gönderilenleri biriktiren bellek içi işçi istemcisi (protokol testleri için).</summary>
public sealed class InMemoryTtsWorkerClient : TtsWorkerClientBase
{
    private readonly StringReader _output;

    /// <summary>İşçinin "yazacağı" çıktıyla kurar.</summary>
    /// <param name="workerOutput">stdout içeriği.</param>
    public InMemoryTtsWorkerClient(string workerOutput)
        : base(NullLogger.Instance)
    {
        _output = new StringReader(workerOutput);
    }

    /// <summary>İşçiye gönderilen satırlar.</summary>
    public StringWriter Sent { get; } = new();

    /// <inheritdoc />
    public override bool IsRunning => true;

    /// <inheritdoc />
    protected override TextWriter Input => Sent;

    /// <inheritdoc />
    protected override TextReader Output => _output;

    /// <inheritdoc />
    protected override ValueTask DisposeCoreAsync()
    {
        _output.Dispose();
        Sent.Dispose();
        return ValueTask.CompletedTask;
    }
}
