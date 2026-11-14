using Konsept.Shared.Kernel.Security;
using Konsept.Shared.Logging;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Konsept.Web.Infrastructure;

/// <summary>
/// Web'in Warning+ loglarını toplu olarak API'ye ileten Serilog sink'i (spec 0008 Req 6; Web veritabanına dokunmaz,
/// Y-5). Mesaj ve exception göndermeden önce maskelenir. Döngü olmasın diye HTTP istemcisinin ve API istemcisinin
/// kendi logları iletilmez; iletim hatası uygulamayı etkilemez (yalnızca Serilog iç loguna düşer). Çerçeve adaptörü.
/// </summary>
/// <param name="services">Kök servis sağlayıcı.</param>
public sealed class ApiLogForwardingSink(IServiceProvider services) : IBatchedLogEventSink
{
    /// <summary>İletilmeyen kategori önekleri.</summary>
    public static readonly string[] ExcludedCategories =
        ["System.Net.Http", "Microsoft.Extensions.Http", "Konsept.Web.ApiClient", "Konsept.Web.Infrastructure.ApiLogForwardingSink"];

    /// <summary>Olay iletilecek mi (seviye Warning+, kategori dışlanmamış).</summary>
    /// <param name="logEvent">Olay.</param>
    /// <returns>İletilecekse <c>true</c>.</returns>
    public static bool ShouldForward(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        var category = Text(logEvent, "SourceContext");
        return logEvent.Level >= LogEventLevel.Warning
            && (category is null || !ExcludedCategories.Any(c => category.StartsWith(c, StringComparison.Ordinal)));
    }

    /// <summary>Serilog olayını maskelenmiş iletim kaydına çevirir.</summary>
    /// <param name="logEvent">Olay.</param>
    /// <returns>Kayıt.</returns>
    public static ForwardedLogRequest ToRequest(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        return new ForwardedLogRequest(
            logEvent.Timestamp,
            logEvent.Level switch { LogEventLevel.Warning => 3, LogEventLevel.Error => 4, LogEventLevel.Fatal => 5, _ => 2 },
            0,
            SecretMasker.Mask(KonseptLogging.RenderMessage(logEvent)),
            logEvent.Exception is null ? null : SecretMasker.Mask(logEvent.Exception.ToString()),
            Text(logEvent, KonseptLogging.CorrelationProperty),
            Text(logEvent, "SourceContext"));
    }

    /// <inheritdoc />
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        var requests = batch.Where(ShouldForward).Select(ToRequest).ToList();
        foreach (var chunk in requests.Chunk(200))
        {
            await using var scope = services.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IKonseptApiClient>().ForwardLogsAsync(chunk, CancellationToken.None).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                SelfLog.WriteLine("Web logları API'ye iletilemedi: {0}", result.Error!.Code);
            }
        }
    }

    private static string? Text(LogEvent logEvent, string property) =>
        logEvent.Properties.TryGetValue(property, out var value)
            ? value is ScalarValue { Value: string s } ? s : value.ToString()
            : null;
}
