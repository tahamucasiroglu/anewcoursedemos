using System.Globalization;
using Serilog;
using Serilog.Events;
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

    private static readonly MessageTemplateTextFormatter MessageOnly = new("{Message:lj}", CultureInfo.InvariantCulture);

    /// <summary>Olayın mesajını dosyadaki gibi işler (metin değerleri tırnaksız, diğerleri JSON).</summary>
    /// <param name="logEvent">Olay.</param>
    /// <returns>Mesaj metni.</returns>
    public static string RenderMessage(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        MessageOnly.Format(logEvent, writer);
        return writer.ToString();
    }

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

    /// <summary>
    /// Günlük dönen, maskeli log dosyasını ekler. Saklama süresini <c>LogFileJanitor</c> uygular. Her dosyanın tek
    /// yazarı vardır; paylaşımlı mod kullanılmaz (Linux'ta adlandırılmış mutex servis kullanıcısının ev klasörünü
    /// ister — Pi'deki <c>anewconcept</c> kullanıcısının ev klasörü yok).
    /// </summary>
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
            retainedFileCountLimit: 31);
    }

    /// <summary>
    /// Yapılandırmadaki gizli girdileri (gizli ad, bağlantı dizesi şifresi) bilinen sır olarak kaydeder; bu değerler
    /// logda hangi biçimde geçerse geçsin maskelenir (hunt r3, H3-H). Uygulama başlarken bir kez çağrılır.
    /// </summary>
    /// <param name="entries">Yapılandırma girdileri (<c>IConfiguration.AsEnumerable()</c>).</param>
    public static void RegisterKnownSecrets(IEnumerable<KeyValuePair<string, string?>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        foreach (var (key, value) in entries)
        {
            Kernel.Security.SecretMasker.RegisterConfigurationValue(key, value);
        }
    }
}
