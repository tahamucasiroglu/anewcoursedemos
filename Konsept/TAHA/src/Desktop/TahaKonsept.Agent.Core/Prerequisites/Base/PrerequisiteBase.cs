using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Base;

/// <summary>
/// Ön gereksinim tabanı: kimlik/ad/grup bilgileri ve ilerleme bildirme yardımcısı (<see cref="Report"/>).
/// Varsayılan grup <c>Temel</c>, isteğe bağlı değil.
/// </summary>
public abstract class PrerequisiteBase : IPrerequisite
{
    /// <summary>Temel grup adı.</summary>
    public const string CoreGroup = "Temel";

    /// <summary>Python + CUDA grup adı (Chatterbox ve diğer Python motorları).</summary>
    public const string PythonGroup = "Python + CUDA (Chatterbox vb.)";

    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public virtual bool IsOptional => false;

    /// <inheritdoc />
    public virtual string Group => CoreGroup;

    /// <inheritdoc />
    public abstract int Order { get; }

    /// <inheritdoc />
    public virtual string? InstalledVersion => null;

    /// <inheritdoc />
    public abstract bool IsInstalled();

    /// <inheritdoc />
    public abstract Task<Result> InstallAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct);

    /// <summary>İlerleme bildirir.</summary>
    /// <param name="progress">Hedef (boş olabilir).</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="percent">Yüzde.</param>
    protected static void Report(IProgress<EngineSetupProgress>? progress, string message, double? percent) =>
        progress?.Report(new EngineSetupProgress(message, percent is null ? null : Math.Clamp(percent.Value, 0d, 100d)));
}
