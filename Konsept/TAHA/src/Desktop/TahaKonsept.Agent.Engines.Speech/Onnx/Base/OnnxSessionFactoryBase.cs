using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using TahaKonsept.Agent.Engines.Speech.Onnx.Abstract;

namespace TahaKonsept.Agent.Engines.Speech.Onnx.Base;

/// <summary>
/// <see cref="IOnnxSessionFactory"/> ortak davranışı (GPU'suz test edilebilir): <see cref="Candidates"/> sırasıyla (GPU isteniyorsa
/// <c>DirectMl → Cpu</c>, değilse yalnız <c>Cpu</c>) her sağlayıcı için yeni <see cref="SessionOptions"/> kurar,
/// <see cref="ConfigureProvider"/> ile sağlayıcıyı ekler ve oturumları açar. Sağlayıcı/oturum hatası (<see cref="IsProviderFailure"/>)
/// sonraki adaya geçirir; GPU'dan CPU'ya her düşüş nedeniyle <c>Warning</c> loglanır. Son aday da düşerse hata yukarı fırlatılır.
/// Somut sınıf yalnız sağlayıcının nasıl ekleneceğini bilir.
/// </summary>
public abstract class OnnxSessionFactoryBase : IOnnxSessionFactory
{
    private static readonly OnnxExecutionProvider[] GpuFirst = [OnnxExecutionProvider.DirectMl, OnnxExecutionProvider.Cpu];
    private static readonly OnnxExecutionProvider[] CpuOnly = [OnnxExecutionProvider.Cpu];

    /// <summary>Günlükçü ile kurar.</summary>
    /// <param name="logger">Günlükçü (düşüş uyarıları).</param>
    protected OnnxSessionFactoryBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>Günlükçü.</summary>
    protected ILogger Logger { get; }

    /// <summary>Denenecek sağlayıcılar (sıralı).</summary>
    /// <param name="preferGpu">GPU isteniyor mu.</param>
    /// <returns>Sağlayıcı sırası.</returns>
    public static IReadOnlyList<OnnxExecutionProvider> Candidates(bool preferGpu) => preferGpu ? GpuFirst : CpuOnly;

    /// <summary>Hata bir sağlayıcı/oturum açma hatası mı (sonraki sağlayıcıya geçmeye değer).</summary>
    /// <param name="exception">Hata.</param>
    /// <returns>Sağlayıcı hatasıysa <c>true</c>.</returns>
    public static bool IsProviderFailure(Exception exception) =>
        exception is OnnxRuntimeException or DllNotFoundException or EntryPointNotFoundException or BadImageFormatException
            or InvalidOperationException or NotSupportedException or System.Runtime.InteropServices.SEHException;

    /// <inheritdoc />
    public OnnxLoadResult<T> Load<T>(bool preferGpu, Func<SessionOptions, T> open)
    {
        ArgumentNullException.ThrowIfNull(open);
        var candidates = Candidates(preferGpu);
        string? fallbackReason = null;
        for (var i = 0; i < candidates.Count; i++)
        {
            var provider = candidates[i];
            var isLast = i == candidates.Count - 1;
            var options = new SessionOptions();
            try
            {
                ConfigureProvider(options, provider);
                var value = open(options);
                if (fallbackReason is not null)
                {
                    Logger.LogWarning("ONNX: GPU yerine {Provider} kullanılıyor — {Reason}", provider, fallbackReason);
                }

                return new OnnxLoadResult<T>(value, provider, provider == OnnxExecutionProvider.Cpu ? fallbackReason : null);
            }
            catch (Exception ex) when (!isLast && IsProviderFailure(ex))
            {
                fallbackReason = $"{provider} açılamadı ({ex.GetType().Name}: {ex.Message})";
                Logger.LogWarning(ex, "ONNX: {Provider} sağlayıcısı kullanılamadı, sonraki sağlayıcı deneniyor", provider);
            }
            finally
            {
                options.Dispose();
            }
        }

        throw new InvalidOperationException("ONNX sağlayıcı listesi boş.");
    }

    /// <summary>Sağlayıcıyı oturum seçeneklerine ekler (ve sağlayıcıya özgü ayarları yapar).</summary>
    /// <param name="options">Oturum seçenekleri.</param>
    /// <param name="provider">Sağlayıcı.</param>
    protected abstract void ConfigureProvider(SessionOptions options, OnnxExecutionProvider provider);
}
