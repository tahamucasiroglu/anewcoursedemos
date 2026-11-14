using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using TahaKonsept.Agent.Engines.Speech.Onnx;
using TahaKonsept.Agent.Engines.Speech.Onnx.Base;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>
/// GPU'suz <see cref="OnnxSessionFactoryBase"/> testi: gerçek sağlayıcı eklemez; <see cref="FailingProvider"/> verilirse o sağlayıcıda
/// (sürücü hatası gibi) <see cref="InvalidOperationException"/> fırlatır ve hangi sağlayıcıların denendiğini kaydeder (<see cref="Tried"/>).
/// </summary>
public sealed class FakeOnnxSessionFactory : OnnxSessionFactoryBase
{
    /// <summary>Günlükçü ile kurar.</summary>
    /// <param name="logger">Günlükçü.</param>
    /// <param name="failingProvider">Eklenirken hata verecek sağlayıcı (<c>null</c> = hiçbiri).</param>
    public FakeOnnxSessionFactory(ILogger logger, OnnxExecutionProvider? failingProvider)
        : base(logger)
    {
        FailingProvider = failingProvider;
    }

    /// <summary>Hata verecek sağlayıcı.</summary>
    public OnnxExecutionProvider? FailingProvider { get; }

    /// <summary>Denenen sağlayıcılar (sıralı).</summary>
    public List<OnnxExecutionProvider> Tried { get; } = [];

    /// <inheritdoc />
    protected override void ConfigureProvider(SessionOptions options, OnnxExecutionProvider provider)
    {
        Tried.Add(provider);
        if (provider == FailingProvider)
        {
            throw new InvalidOperationException("DirectML aygıtı oluşturulamadı (test)");
        }
    }
}
