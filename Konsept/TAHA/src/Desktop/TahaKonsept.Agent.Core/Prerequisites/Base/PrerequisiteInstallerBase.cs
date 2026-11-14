using TahaKonsept.Agent.Core.Execution;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Base;

/// <summary>
/// Kurulum yöneticisi tabanı: bileşenleri <see cref="IPrerequisite.Order"/>'a göre sıralar, her adımın ilerlemesini genel yüzdeye çevirir
/// (<c>(adım + adımYüzdesi/100) / adımSayısı</c>). Zorunlu adım hatası kurulumu durdurur; isteğe bağlı hata kaydedilip devam edilir.
/// </summary>
public abstract class PrerequisiteInstallerBase : IPrerequisiteInstaller
{
    private readonly IReadOnlyList<IPrerequisite> _prerequisites;

    /// <summary>Oluşturur.</summary>
    /// <param name="prerequisites">Bileşenler.</param>
    protected PrerequisiteInstallerBase(IEnumerable<IPrerequisite> prerequisites)
    {
        ArgumentNullException.ThrowIfNull(prerequisites);
        _prerequisites = prerequisites.OrderBy(p => p.Order).ThenBy(p => p.Id, StringComparer.Ordinal).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<PrerequisiteStatus> GetStatus() =>
        _prerequisites
            .Select(p => new PrerequisiteStatus(p.Id, p.DisplayName, p.Description, p.Group, p.IsOptional, SafeInstalled(p), p.InstalledVersion))
            .ToList();

    /// <inheritdoc />
    public bool AreRequiredInstalled() => _prerequisites.Where(p => !p.IsOptional).All(SafeInstalled);

    /// <inheritdoc />
    public Task<Result> InstallAsync(IReadOnlyCollection<string>? optionalIds, IProgress<PrerequisiteProgress>? progress, CancellationToken ct)
    {
        var selected = optionalIds ?? [];
        var steps = _prerequisites
            .Where(p => !p.IsOptional || selected.Contains(p.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();
        return RunStepsAsync(steps, progress, ct);
    }

    /// <inheritdoc />
    public Task<Result> InstallOneAsync(string id, IProgress<PrerequisiteProgress>? progress, CancellationToken ct)
    {
        var prerequisite = _prerequisites.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        return prerequisite is null
            ? Task.FromResult<Result>(Error.NotFound("agent.prerequisite_not_found", "Bileşen bulunamadı: " + id))
            : RunStepsAsync([prerequisite], progress, ct);
    }

    /// <summary>Adım sonucu (loglama için).</summary>
    /// <param name="prerequisite">Bileşen.</param>
    /// <param name="result">Sonuç.</param>
    protected virtual void OnStepCompleted(IPrerequisite prerequisite, Result result)
    {
    }

    private async Task<Result> RunStepsAsync(IReadOnlyList<IPrerequisite> steps, IProgress<PrerequisiteProgress>? progress, CancellationToken ct)
    {
        var failures = new List<string>();
        for (var index = 0; index < steps.Count; index++)
        {
            ct.ThrowIfCancellationRequested();
            var step = steps[index];
            var stepIndex = index;
            void Emit(double? percent, string message, string? log) =>
                progress?.Report(new PrerequisiteProgress(
                    step.Id,
                    step.DisplayName,
                    stepIndex,
                    steps.Count,
                    percent,
                    Math.Clamp((stepIndex + ((percent ?? 0d) / 100d)) * 100d / steps.Count, 0d, 100d),
                    message,
                    log));

            Emit(0, step.DisplayName + " denetleniyor…", null);
            Result result;
            try
            {
                var stepProgress = new InlineProgress<Abstractions.Engines.EngineSetupProgress>(p =>
                    Emit(p.Percent, p.Percent is null ? step.DisplayName + " kuruluyor…" : p.Message, p.Percent is null ? p.Message : null));
                result = await step.InstallAsync(stepProgress, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result = Error.Failure("agent.prerequisite_install_failed", step.DisplayName + " kurulurken hata: " + ex.Message);
            }

            OnStepCompleted(step, result);
            if (result.IsSuccess)
            {
                Emit(100, step.DisplayName + " hazır.", null);
                continue;
            }

            Emit(null, step.DisplayName + " kurulamadı.", result.Error.Message);
            failures.Add(step.DisplayName + ": " + result.Error.Message);
            if (!step.IsOptional)
            {
                return Error.Failure("agent.prerequisite_install_failed", string.Join(Environment.NewLine, failures));
            }
        }

        return failures.Count == 0
            ? Result.Success()
            : Error.Failure("agent.prerequisite_partial", "Bazı isteğe bağlı bileşenler kurulamadı:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    private static bool SafeInstalled(IPrerequisite prerequisite)
    {
        try
        {
            return prerequisite.IsInstalled();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
