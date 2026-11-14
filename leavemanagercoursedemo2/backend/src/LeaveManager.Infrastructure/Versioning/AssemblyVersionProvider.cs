using System.Reflection;
using LeaveManager.Application.Abstractions;

namespace LeaveManager.Infrastructure.Versioning;

/// <summary>Reads the version from the entry assembly (set once in backend/Directory.Build.props).</summary>
public sealed class AssemblyVersionProvider : IAppVersionProvider
{
    private readonly string _version;

    public AssemblyVersionProvider()
        : this(Assembly.GetEntryAssembly() ?? typeof(AssemblyVersionProvider).Assembly)
    {
    }

    public AssemblyVersionProvider(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        // Drop the "+<commit>" source-link suffix: the commit is an internal detail (spec 0001, AC-5).
        _version = informational?.Split('+')[0] ?? assembly.GetName().Version?.ToString() ?? "unknown";
    }

    public string GetVersion() => _version;
}
