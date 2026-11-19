using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction;

namespace OkulYonetim.Infrastructure.Logging;

/// <summary>
/// nArchitecture'ın log arayüzünü (<see cref="ILogger"/>; işlem log pipeline'ı bunu kullanır) uygulamanın
/// Serilog yapılandırmasına bağlar (ADR-0003). Mesajlar şablon değil, değer olarak yazılır.
/// </summary>
public sealed class SerilogLoggerAdapter : ILogger
{
    private const string Template = "{NArchitectureMessage}";
    private readonly Serilog.ILogger logger;

    public SerilogLoggerAdapter(Serilog.ILogger logger)
    {
        this.logger = logger.ForContext("SourceContext", "NArchitecture");
    }

    public void Trace(string message) => this.logger.Verbose(Template, message);

    public void Critical(string message) => this.logger.Fatal(Template, message);

    public void Information(string message) => this.logger.Information(Template, message);

    public void Warning(string message) => this.logger.Warning(Template, message);

    public void Debug(string message) => this.logger.Debug(Template, message);

    public void Error(string message) => this.logger.Error(Template, message);
}
