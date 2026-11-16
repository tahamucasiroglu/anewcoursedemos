using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// İsteğe göre yanıt veren sahte sağlayıcı (plan 0014; gerçek sağlayıcı çağrısı yok). Her denemede <see cref="Handler"/> çağrının rolüne
/// (talimattan) ve sırasına bakarak yanıt üretir. Tüm çağrılar <see cref="Calls"/>'ta tutulur.
/// </summary>
public sealed class ScriptedProviderClient : IProviderClient
{
    private readonly Lock _gate = new();
    private readonly List<ProviderCall> _calls = [];

    /// <summary>Gets or sets yanıt üretici: çağrı, rol ve o roldeki sıra (1'den).</summary>
    public Func<ProviderCall, CallRole, int, Task<ProviderAttempt>> Handler { get; set; } =
        (_, _, _) => Task.FromResult(ProviderAttempt.Completed("{}", 10, 10));

    /// <summary>Gets yapılan çağrılar.</summary>
    public IReadOnlyList<ProviderCall> Calls
    {
        get
        {
            lock (_gate)
            {
                return [.. _calls];
            }
        }
    }

    /// <summary>Bir çağrının rolü.</summary>
    /// <param name="call">Çağrı.</param>
    /// <returns>Rol.</returns>
    public static CallRole RoleOf(ProviderCall call)
    {
        ArgumentNullException.ThrowIfNull(call);
        var system = call.System ?? string.Empty;
        return call.Model.Kind == Konsept.Modules.AiGateway.Domain.Enums.ModelKind.Image ? CallRole.Image
            : system.Contains(Konsept.Modules.Production.Domain.Rules.PipelinePrompts.AnalysisTask, StringComparison.Ordinal) ? CallRole.Analysis
            : system.Contains(Konsept.Modules.Production.Domain.Rules.PipelinePrompts.FixTask, StringComparison.Ordinal) ? CallRole.Fix
            : system.Contains(Konsept.Modules.Production.Domain.Rules.PipelinePrompts.CodeTask, StringComparison.Ordinal) ? CallRole.Code
            : system.Contains("Task: suggest new video topics", StringComparison.Ordinal) ? CallRole.Topics
            : system.Contains("Task: write one complete video concept", StringComparison.Ordinal) ? CallRole.Concept
            : system.Contains("You check video topics for duplicates", StringComparison.Ordinal) ? CallRole.Dedupe
            : CallRole.Other;
    }

    /// <summary>Roldeki çağrılar.</summary>
    /// <param name="role">Rol.</param>
    /// <returns>Çağrılar.</returns>
    public IReadOnlyList<ProviderCall> CallsOf(CallRole role) => [.. Calls.Where(c => RoleOf(c) == role)];

    /// <summary>Çağrıları temizler, varsayılan yanıtçıya döner.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            _calls.Clear();
        }

        Handler = (_, _, _) => Task.FromResult(ProviderAttempt.Completed("{}", 10, 10));
    }

    /// <inheritdoc />
    public Task<ProviderAttempt> SendAsync(ProviderCall call, CancellationToken cancellationToken)
    {
        var role = RoleOf(call);
        int index;
        lock (_gate)
        {
            _calls.Add(call);
            index = _calls.Count(c => RoleOf(c) == role);
        }

        return Handler(call, role, index);
    }
}
