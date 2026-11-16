using Konsept.Agent.Application.Abstract;
using Serilog.Core;
using Serilog.Events;

namespace Konsept.Agent.Infrastructure.Logging;

/// <summary>
/// Ajanda bir Error/Critical log oluştuğunda "Bir hata oldu" uyarısını tetikler (spec 0022 Req 4): özet = mesaj (+ exception türü ve
/// mesajı). Merkez tekrarları ayıklar ve yalnızca Information/Warning yazar — döngü olmaz. Uyarı log iş parçacığını bekletmez.
/// Çerçeve adaptörü (Serilog sink'i).
/// </summary>
public sealed class ErrorAlertSink : ILogEventSink
{
    /// <summary>
    /// Bu özelliği taşıyan Error olayı uyarı açmaz (spec 0009 Req 14: kurulum hatası satırda gösterilir, kullanıcı "Hatayı bildir" ile
    /// bileşen ve adımla açar).
    /// </summary>
    public const string NoAlertProperty = "NoAlert";

    private IErrorReportCenter? _center;

    /// <summary>Uyarıların gideceği merkezi bağlar (bileşim kökünde).</summary>
    /// <param name="center">Merkez.</param>
    public void Attach(IErrorReportCenter center) => Volatile.Write(ref _center, center);

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        var center = Volatile.Read(ref _center);
        if (center is null || logEvent.Level < LogEventLevel.Error || logEvent.Properties.ContainsKey(NoAlertProperty))
        {
            return;
        }

        var summary = Konsept.Shared.Logging.KonseptLogging.RenderMessage(logEvent);
        if (logEvent.Exception is not null)
        {
            summary = $"{summary} ({logEvent.Exception.GetType().Name}: {logEvent.Exception.Message})";
        }

        _ = Task.Run(() => center.Raise(summary));
    }
}
