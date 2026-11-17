using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Infrastructure.Base;
using Serilog;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Yerel model aşamaları (spec 0028); mantık <see cref="LocalModelStagesBase"/>'de. Çalıştırma betikleri ajana gömülüdür
/// (<c>Setup/runners/</c>) ve <c>runners\</c> klasörüne çıkarılır.
/// </summary>
/// <param name="catalog">Çözülmüş bileşen listesi.</param>
/// <param name="setup">Kurulum durumu.</param>
/// <param name="installRoot">Kurulum yeri.</param>
/// <param name="processes">Süreç çalıştırıcı.</param>
/// <param name="logger">Teknik log.</param>
public sealed class LocalModelStages(IReadOnlyList<SetupComponent> catalog, ISetupStateStore setup, string installRoot, IProcessRunner processes, ILogger logger)
    : LocalModelStagesBase(catalog, setup, installRoot, processes, logger)
{
    /// <summary>Gömülü betik kaynaklarının öneki.</summary>
    public const string ScriptResourcePrefix = "Konsept.Agent.Infrastructure.Setup.runners.";

    /// <inheritdoc />
    protected override string ExtractScript(string name)
    {
        using var source = typeof(LocalModelStages).Assembly.GetManifestResourceStream(ScriptResourcePrefix + name)
            ?? throw new InvalidOperationException("Çalıştırma betiği ajana gömülü değil: " + name);
        Directory.CreateDirectory(RunnersDirectory);
        var path = Path.Combine(RunnersDirectory, name);
        using var target = File.Create(path);
        source.CopyTo(target);
        return path;
    }
}
