using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Konsept.Shared.Infrastructure.Logging;

/// <summary>
/// Serilog temel kurulumu: konsol + günlük dönen dosya, 7 gün saklama (docs/conventions.md, Loglama).
/// Maskeleme ve DB sink 0008'de gelir.
/// </summary>
public static class SerilogSetup
{
    /// <summary>Uygulamaya Serilog'u ekler.</summary>
    /// <param name="builder">Web uygulaması kurucusu.</param>
    /// <param name="applicationName">Log dosyası öneki (ör. <c>api</c>).</param>
    public static void AddKonseptSerilog(this WebApplicationBuilder builder, string applicationName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var directory = builder.Configuration["Logging:Directory"] ?? Path.Combine(AppContext.BaseDirectory, "logs");
        builder.Services.AddSerilog((_, logger) => logger
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(directory, $"{applicationName}-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                formatProvider: System.Globalization.CultureInfo.InvariantCulture));
    }
}
