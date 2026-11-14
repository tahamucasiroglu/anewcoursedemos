using LeaveManager.Application.Abstractions;

namespace LeaveManager.Application.Health;

/// <summary>The health answer: only the status and the version, no other internals (spec 0001, AC-5).</summary>
public sealed record SystemHealth(string Status, string Version)
{
    public const string Healthy = "healthy";
}

/// <summary>Use case: "are you running?" — answered without identity (spec 0001, R-2).</summary>
public sealed class GetSystemHealth
{
    private readonly IAppVersionProvider _versionProvider;

    public GetSystemHealth(IAppVersionProvider versionProvider)
    {
        ArgumentNullException.ThrowIfNull(versionProvider);
        _versionProvider = versionProvider;
    }

    public SystemHealth Execute() => new(SystemHealth.Healthy, _versionProvider.GetVersion());
}
