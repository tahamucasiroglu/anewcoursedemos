using System.Globalization;
using Serilog;
using Serilog.Formatting.Display;

namespace Konsept.Shared.Logging;

/// <summary>
/// Web, Api ve ajanın ortak teknik log kurulumu (spec 0008 Req 1, 4, 5, 8): kaynak uygulama, UTC zaman, korelasyon
/// numarası ve maskeleme; günlük dönen dosya <c>&lt;klasör&gt;/&lt;önek&gt;-yyyyMMdd.log</c>.
/// </summary>
public static class KonseptLogging
{
    /// <summary>Kaynak uygulama özelliğinin adı.</summary>
    public const string ApplicationProperty = "Application";

    /// <summary>Korelasyon numarası özelliğinin adı.</summary>
    public const string CorrelationProperty = "CorrelationId";

    /// <summary>Dosya ve konsol satır şablonu: UTC zaman, seviye, kaynak, korelasyon, mesaj, exception.</summary>
    public const string OutputTemplate =
        "{UtcTimestamp:yyyy-MM-ddTHH:mm:ss.fffZ} [{Level:u3}] {Application} {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>Ortak zenginleştiricileri ekler; maskeleme en sondadır.</summary>
    /// <param name="configuration">Logger yapılandırması.</param>
    /// <param name="application">Kaynak uygulama (<see cref="LogSources"/>).</param>
    /// <returns>Aynı yapılandırma.</returns>
    public static LoggerConfiguration WithKonseptEnrichment(this LoggerConfiguration configuration, string application)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration
            .Enrich.FromLogContext()
            .Enrich.WithProperty(ApplicationProperty, application)
            .Enrich.With(new UtcTimestampEnricher())
            .Enrich.With(new MaskingEnricher());
    }

    /// <summary>Maskeleyen satır biçimlendiricisini oluşturur.</summary>
    /// <returns>Biçimlendirici.</returns>
    public static MaskingTextFormatter CreateFormatter() =>
        new(new MessageTemplateTextFormatter(OutputTemplate, CultureInfo.InvariantCulture));

    /// <summary>Günlük dönen, maskeli log dosyasını ekler. Saklama süresini <c>LogFileJanitor</c> uygular.</summary>
    /// <param name="configuration">Logger yapılandırması.</param>
    /// <param name="directory">Log klasörü.</param>
    /// <param name="prefix">Dosya öneki (ör. <c>api</c>).</param>
    /// <returns>Aynı yapılandırma.</returns>
    public static LoggerConfiguration WriteToKonseptFile(this LoggerConfiguration configuration, string directory, string prefix)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.WriteTo.File(
            CreateFormatter(),
            Path.Combine(directory, $"{prefix}-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31,
            shared: true);
    }
}
