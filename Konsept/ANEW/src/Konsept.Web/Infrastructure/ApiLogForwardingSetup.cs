using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Konsept.Web.Infrastructure;

/// <summary>Web log iletim sink'inin Serilog'a eklenmesi (spec 0008 Req 6).</summary>
public static class ApiLogForwardingSetup
{
    /// <summary>Warning+ logları toplu olarak API'ye ileten sink'i ekler.</summary>
    /// <param name="configuration">Logger yapılandırması.</param>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <returns>Aynı yapılandırma.</returns>
    public static LoggerConfiguration WriteToApiForwarding(this LoggerConfiguration configuration, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.WriteTo.Sink(
            new ApiLogForwardingSink(services),
            new BatchingOptions { BatchSizeLimit = 200, BufferingTimeLimit = TimeSpan.FromSeconds(2), QueueLimit = 10_000 },
            LogEventLevel.Warning);
    }
}
